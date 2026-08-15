using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Auth.TwoFactor;

public sealed record SetTwoFactorCommand(bool Enabled) : ICommand<ErrorOr<Success>>;

public sealed class SetTwoFactorCommandHandler(
    ICurrentUserService currentUser,
    IUserAccountService accounts)
    : ICommandHandler<SetTwoFactorCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(SetTwoFactorCommand command, CancellationToken ct)
    {
        await accounts.SetTwoFactorEnabledAsync(currentUser.User.Id, command.Enabled, ct);
        return Result.Success;
    }
}
