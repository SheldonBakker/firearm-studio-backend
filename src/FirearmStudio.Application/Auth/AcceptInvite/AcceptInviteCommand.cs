using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Auth.AcceptInvite;

public sealed record AcceptInviteCommand(AcceptInviteRequest Request)
    : ICommand<ErrorOr<AuthTokensResponse>>;

public sealed class AcceptInviteCommandHandler(
    IUserAccountService accounts,
    IOtpService otp,
    ITokenService tokens,
    IApplicationDbContext db,
    ITenantContext tenant)
    : ICommandHandler<AcceptInviteCommand, ErrorOr<AuthTokensResponse>>
{
    public async Task<ErrorOr<AuthTokensResponse>> Handle(
        AcceptInviteCommand command,
        CancellationToken cancellationToken)
    {
        var address = command.Request.Email.Trim().ToLowerInvariant();

        var account = await accounts.FindByEmailAsync(address, cancellationToken);
        if (account is null)
        {
            return Error.Validation(AuthErrorCodes.CodeInvalid, "The code is not valid.");
        }

        var result = await otp.VerifyAsync(
            account.Id, OtpPurpose.Invite, command.Request.Code, cancellationToken);

        var failure = AuthResults.ToError(result);
        if (failure is not null)
        {
            return failure.Value;
        }

        var errors = await accounts.SetPasswordAsync(
            account.Id, command.Request.Password, cancellationToken);

        if (errors.Count > 0)
        {
            return Error.Validation(
                AuthErrorCodes.PasswordRejected,
                string.Join(" ", errors));
        }

        await accounts.ConfirmEmailAsync(account.Id, cancellationToken);

        await AppUserLinker.LinkAsync(db, tenant, account.Id, address, cancellationToken);

        var effectivePhone = string.IsNullOrEmpty(command.Request.PhoneNumber)
            ? null
            : command.Request.PhoneNumber.Trim();

        var alreadyHasProvenNumber = account.PhoneNumberConfirmed;

        using (tenant.BeginBypass())
        {
            var appUser = await db.AppUsers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.AuthUserId == account.Id, cancellationToken);

            if (appUser is not null)
            {
                effectivePhone ??= appUser.PhoneNumber;

                if (!alreadyHasProvenNumber && !string.IsNullOrEmpty(effectivePhone))
                {
                    appUser.PhoneNumber = effectivePhone;
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
        }

        if (!alreadyHasProvenNumber && !string.IsNullOrEmpty(effectivePhone))
        {
            await accounts.SetPhoneNumberAsync(account.Id, effectivePhone, confirmed: false, cancellationToken);
        }

        var pair = await tokens.IssueAsync(account.Id, account.Email, cancellationToken);

        return new AuthTokensResponse(pair.AccessToken, pair.RefreshToken, pair.AccessExpiresAt);
    }
}
