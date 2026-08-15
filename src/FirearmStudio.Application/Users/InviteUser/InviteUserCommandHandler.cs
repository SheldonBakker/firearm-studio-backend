using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Users.InviteUser;

public sealed class InviteUserCommandHandler(
    IApplicationDbContext db,
    ITenantContext tenant,
    IUserAccountService accounts,
    IOtpService otp,
    IEmailSender emailSender)
    : ICommandHandler<InviteUserCommand, ErrorOr<AppUserResponse>>
{
    public async Task<ErrorOr<AppUserResponse>> Handle(
        InviteUserCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (!Enum.IsDefined(request.Role))
        {
            return Error.Validation(ErrorCodes.UnknownRole, "Unknown role.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var newCompanyId = tenant.CompanyId!.Value; // endpoint is [Authorize(Roles = Admin)] → always set

        using (tenant.BeginBypass())
        {
            var existing = await db.AppUsers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

            if (existing is not null)
            {
                // Don't orphan the source company of its last admin (skip when staying in same company).
                if (existing.CompanyId != newCompanyId
                    && existing.Role == AppRole.Admin
                    && existing.IsActive)
                {
                    var sourceActiveAdmins = await db.AppUsers
                        .IgnoreQueryFilters()
                        .CountAsync(
                            user => user.CompanyId == existing.CompanyId && user.Role == AppRole.Admin && user.IsActive,
                            cancellationToken);
                    if (sourceActiveAdmins <= 1)
                    {
                        return Error.Conflict(
                            ErrorCodes.SourceLastActiveAdmin,
                            "That user is the last active admin of their current company and cannot be reassigned.");
                    }
                }

                existing.CompanyId = newCompanyId;
                existing.Role = request.Role;
                existing.IsActive = true;
                existing.InvitedAt = DateTime.UtcNow;
                // auth_user_id, linked_at, full_name left intact so the user stays linked.

                await db.SaveChangesAsync(cancellationToken); // inside bypass → guard permits the move

                // Already linked means they have working credentials; moving them between
                // companies does not require proving the mailbox again.
                if (existing.AuthUserId is null)
                {
                    await ProvisionAndInviteAsync(email, cancellationToken);
                }

                return AppUserResponse.FromEntity(existing);
            }
        }

        var user = new AppUser
        {
            Email = email,
            FullName = request.FullName,
            Role = request.Role,
            IsActive = true,
            InvitedAt = DateTime.UtcNow,
        };

        await db.AppUsers.AddAsync(user, cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Error.Conflict(ErrorCodes.EmailAlreadyExists, "A user with this email already belongs to a company or has a pending invite.");
        }

        await ProvisionAndInviteAsync(email, cancellationToken);

        return AppUserResponse.FromEntity(user);
    }

    /// <summary>
    /// Creates the login account for an invited address and emails a one-time code. The
    /// account has no password until the invitee sets one via accept-invite, so an invite
    /// alone never yields a usable credential.
    /// </summary>
    private async Task ProvisionAndInviteAsync(string address, CancellationToken ct)
    {
        var account = await accounts.FindByEmailAsync(address, ct);

        if (account is null)
        {
            // A random password the invitee never learns. Identity requires one at
            // creation; accept-invite replaces it.
            var placeholder = Guid.NewGuid().ToString("N") + "Aa1!";

            var (created, _) = await accounts.CreateAsync(address, placeholder, ct);
            if (created is null)
            {
                // The invite row is already saved and an admin can resend. Failing the
                // whole command here would leave the caller unable to retry cleanly.
                return;
            }

            account = created;
        }

        var issued = await otp.IssueAsync(account.Id, OtpPurpose.Invite, ct);

        if (issued.Status == OtpIssueStatus.Issued)
        {
            await emailSender.SendOtpAsync(
                address, null, OtpPurpose.Invite, issued.Code!, InviteCodeLifetimeMinutes, ct);
        }
    }

    private const int InviteCodeLifetimeMinutes = 15;

    public static class ErrorCodes
    {
        public const string UnknownRole = "InviteUserCommand.UnknownRole";
        public const string EmailAlreadyExists = "InviteUserCommand.EmailAlreadyExists";
        public const string SourceLastActiveAdmin = "InviteUserCommand.SourceLastActiveAdmin";
    }
}
