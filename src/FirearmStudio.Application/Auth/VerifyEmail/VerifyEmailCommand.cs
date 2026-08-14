using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Auth.VerifyEmail;

public sealed record VerifyEmailCommand(VerifyEmailRequest Request)
    : ICommand<ErrorOr<AuthTokensResponse>>;

public sealed class VerifyEmailCommandHandler(
    IUserAccountService accounts,
    IOtpService otp,
    ITokenService tokens,
    IApplicationDbContext db,
    ITenantContext tenant)
    : ICommandHandler<VerifyEmailCommand, ErrorOr<AuthTokensResponse>>
{
    public async Task<ErrorOr<AuthTokensResponse>> Handle(
        VerifyEmailCommand command,
        CancellationToken cancellationToken)
    {
        var address = command.Request.Email.Trim().ToLowerInvariant();

        var account = await accounts.FindByEmailAsync(address, cancellationToken);
        if (account is null)
        {
            return Error.Validation(AuthErrorCodes.CodeInvalid, "The code is not valid.");
        }

        var result = await otp.VerifyAsync(
            account.Id, OtpPurpose.EmailConfirmation, command.Request.Code, cancellationToken);

        var failure = AuthResults.ToError(result);
        if (failure is not null)
        {
            return failure.Value;
        }

        await accounts.ConfirmEmailAsync(account.Id, cancellationToken);

        await AppUserLinker.LinkAsync(db, tenant, account.Id, address, cancellationToken);

        var pair = await tokens.IssueAsync(account.Id, account.Email, cancellationToken);

        return new AuthTokensResponse(pair.AccessToken, pair.RefreshToken, pair.AccessExpiresAt);
    }
}
