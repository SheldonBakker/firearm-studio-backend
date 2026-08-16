using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Auth.Refresh;

public sealed class RefreshCommandHandler(ITokenService tokens)
    : ICommandHandler<RefreshCommand, ErrorOr<AuthTokensResponse>>
{
    public async Task<ErrorOr<AuthTokensResponse>> Handle(
        RefreshCommand command,
        CancellationToken cancellationToken)
    {
        var (pair, _) = await tokens.RefreshAsync(
            command.Request.RefreshToken, cancellationToken);

        if (pair is null)
        {
            return Error.Unauthorized(
                AuthErrorCodes.RefreshInvalid,
                "That session is no longer valid. Sign in again.");
        }

        return new AuthTokensResponse(pair.AccessToken, pair.RefreshToken, pair.AccessExpiresAt);
    }
}
