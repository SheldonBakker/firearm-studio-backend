using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Auth.Tokens;

public sealed record RefreshCommand(RefreshRequest Request)
    : ICommand<ErrorOr<AuthTokensResponse>>;

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

public sealed record LogoutCommand(LogoutRequest Request) : ICommand<ErrorOr<Success>>;

public sealed class LogoutCommandHandler(ITokenService tokens)
    : ICommandHandler<LogoutCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        await tokens.RevokeAsync(command.Request.RefreshToken, cancellationToken);

        return Result.Success;
    }
}
