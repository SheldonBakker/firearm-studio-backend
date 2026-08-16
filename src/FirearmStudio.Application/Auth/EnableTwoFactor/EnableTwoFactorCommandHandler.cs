using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Auth.EnableTwoFactor;

public sealed class EnableTwoFactorCommandHandler(
    ICurrentUserService currentUser,
    IUserAccountService accounts)
    : ICommandHandler<EnableTwoFactorCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(EnableTwoFactorCommand command, CancellationToken ct)
    {
        await accounts.SetTwoFactorEnabledAsync(currentUser.User.Id, true, ct);
        return Result.Success;
    }
}
