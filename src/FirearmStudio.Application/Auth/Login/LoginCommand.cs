using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Auth.Login;

public sealed record LoginCommand(LoginRequest Request) : ICommand<ErrorOr<AuthTokensResponse>>;

public sealed class LoginCommandHandler(
    IUserAccountService accounts,
    ITokenService tokens)
    : ICommandHandler<LoginCommand, ErrorOr<AuthTokensResponse>>
{
    public async Task<ErrorOr<AuthTokensResponse>> Handle(
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

        var pair = await tokens.IssueAsync(account.Id, account.Email, cancellationToken);

        return new AuthTokensResponse(pair.AccessToken, pair.RefreshToken, pair.AccessExpiresAt);
    }

    private static Error InvalidCredentials => Error.Unauthorized(
        AuthErrorCodes.InvalidCredentials,
        "Email address or password is incorrect.");
}
