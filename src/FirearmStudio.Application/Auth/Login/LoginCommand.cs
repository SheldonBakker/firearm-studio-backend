using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Auth.Login;

public sealed record LoginOutcome(
    AuthTokensResponse? Tokens,
    TwoFactorChallengeResponse? Challenge);

public sealed record LoginCommand(LoginRequest Request) : ICommand<ErrorOr<LoginOutcome>>;

public sealed class LoginCommandHandler(
    IUserAccountService accounts,
    ITokenService tokens,
    IOtpService otp,
    IOtpDispatcher dispatcher)
    : ICommandHandler<LoginCommand, ErrorOr<LoginOutcome>>
{
    private const int CodeLifetimeMinutes = 15;

    public async Task<ErrorOr<LoginOutcome>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var address = command.Request.Email.Trim().ToLowerInvariant();

        var account = await accounts.FindByEmailAsync(address, cancellationToken);
        if (account is null)
        {
            return InvalidCredentials;
        }

        var check = await accounts.CheckPasswordAsync(
            account.Id, command.Request.Password, cancellationToken);

        if (check == PasswordCheckResult.LockedOut)
        {
            return Error.Forbidden(
                AuthErrorCodes.LockedOut,
                "This account is temporarily locked after too many failed attempts. Try again later.");
        }

        if (check == PasswordCheckResult.Failed)
        {
            return InvalidCredentials;
        }

        if (!account.EmailConfirmed)
        {
            return Error.Forbidden(
                AuthErrorCodes.EmailNotConfirmed,
                "Confirm your email address first. Request a new code if yours has expired.");
        }

        if (account.TwoFactorEnabled)
        {
            var issued = await otp.IssueAsync(account.Id, OtpPurpose.TwoFactor, cancellationToken);
            if (issued.Status == OtpIssueStatus.Issued)
            {
                await dispatcher.SendAsync(
                    new OtpRecipient(account.Email, null, account.PhoneNumber),
                    OtpPurpose.TwoFactor,
                    issued.Code!,
                    CodeLifetimeMinutes,
                    cancellationToken);
            }

            var preAuth = tokens.IssuePreAuthToken(account.Id, account.Email);
            return new LoginOutcome(
                Tokens: null,
                Challenge: new TwoFactorChallengeResponse(RequiresTwoFactor: true, PreAuthToken: preAuth));
        }

        var pair = await tokens.IssueAsync(account.Id, account.Email, cancellationToken);
        return new LoginOutcome(
            Tokens: new AuthTokensResponse(pair.AccessToken, pair.RefreshToken, pair.AccessExpiresAt),
            Challenge: null);
    }

    private static Error InvalidCredentials => Error.Unauthorized(
        AuthErrorCodes.InvalidCredentials,
        "Email address or password is incorrect.");
}
