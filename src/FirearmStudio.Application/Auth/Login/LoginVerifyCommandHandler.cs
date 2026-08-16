using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Auth.Login;

public sealed class LoginVerifyCommandHandler(
    ITokenService tokens,
    IOtpService otp)
    : ICommandHandler<LoginVerifyCommand, ErrorOr<AuthTokensResponse>>
{
    public async Task<ErrorOr<AuthTokensResponse>> Handle(
        LoginVerifyCommand command,
        CancellationToken cancellationToken)
    {
        var principal = tokens.ValidatePreAuthToken(command.Request.PreAuthToken);
        if (principal is null)
        {
            return Error.Unauthorized(
                AuthErrorCodes.PreAuthInvalid,
                "Your login session has expired. Sign in again.");
        }

        var result = await otp.VerifyAsync(
            principal.UserId, OtpPurpose.TwoFactor, command.Request.Code, cancellationToken);

        var failure = AuthResults.ToError(result);
        if (failure is not null)
        {
            return failure.Value;
        }

        var pair = await tokens.IssueAsync(principal.UserId, principal.Email, cancellationToken);
        return new AuthTokensResponse(pair.AccessToken, pair.RefreshToken, pair.AccessExpiresAt);
    }
}
