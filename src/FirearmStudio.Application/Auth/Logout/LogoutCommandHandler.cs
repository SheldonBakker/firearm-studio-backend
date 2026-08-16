using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Auth.Logout;

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
