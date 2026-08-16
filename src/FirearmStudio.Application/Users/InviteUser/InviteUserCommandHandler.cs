using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Users.InviteUser;

public sealed class InviteUserCommandHandler(
    IApplicationDbContext db,
    ITenantContext tenant,
    IUserAccountService accounts,
    IOtpService otp,
    IOtpDispatcher dispatcher)
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
        var newCompanyId = tenant.CompanyId!.Value;

        using (tenant.BeginBypass())
        {
            var existing = await db.AppUsers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

            if (existing is not null)
            {
                if (await IsLastActiveAdminAtSourceCompanyAsync(existing, newCompanyId, cancellationToken))
                {
                    return Error.Conflict(
                        ErrorCodes.SourceLastActiveAdmin,
                        "That user is the last active admin of their current company and cannot be reassigned.");
                }

                existing.CompanyId = newCompanyId;
                existing.Role = request.Role;
                existing.IsActive = true;
                existing.InvitedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    existing.PhoneNumber = request.PhoneNumber;
                }

                await db.SaveChangesAsync(cancellationToken);

                var userAlreadyHasVerifiedCredentials = existing.AuthUserId is not null;
                if (!userAlreadyHasVerifiedCredentials)
                {
                    await ProvisionAndInviteAsync(email, existing.PhoneNumber, cancellationToken);
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
            PhoneNumber = request.PhoneNumber,
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

        await ProvisionAndInviteAsync(email, request.PhoneNumber, cancellationToken);

        return AppUserResponse.FromEntity(user);
    }

    private async Task ProvisionAndInviteAsync(string address, string? phone, CancellationToken ct)
    {
        var account = await accounts.FindByEmailAsync(address, ct);

        var deliverToMailboxOnly = account is not null;

        if (account is null)
        {
            var invitePlaceholderPassword = Guid.NewGuid().ToString("N") + "Aa1!";

            var (created, _) = await accounts.CreateAsync(address, invitePlaceholderPassword, ct);
            if (created is null)
            {
                return;
            }

            account = created;
        }

        var issued = await otp.IssueAsync(account.Id, OtpPurpose.Invite, ct);

        if (issued.Status == OtpIssueStatus.Issued)
        {
            await dispatcher.SendAsync(
                new OtpRecipient(address, null, deliverToMailboxOnly ? null : phone),
                OtpPurpose.Invite,
                issued.Code!,
                OtpConstants.CodeLifetimeMinutes,
                ct);
        }
    }

    private async Task<bool> IsLastActiveAdminAtSourceCompanyAsync(
        AppUser user, Guid newCompanyId, CancellationToken ct)
    {
        if (user.CompanyId == newCompanyId || user.Role != AppRole.Admin || !user.IsActive)
        {
            return false;
        }

        var sourceActiveAdmins = await db.AppUsers
            .IgnoreQueryFilters()
            .CountAsync(u => u.CompanyId == user.CompanyId && u.Role == AppRole.Admin && u.IsActive, ct);

        return sourceActiveAdmins <= 1;
    }

    public static class ErrorCodes
    {
        public const string UnknownRole = "InviteUserCommand.UnknownRole";
        public const string EmailAlreadyExists = "InviteUserCommand.EmailAlreadyExists";
        public const string SourceLastActiveAdmin = "InviteUserCommand.SourceLastActiveAdmin";
    }
}
