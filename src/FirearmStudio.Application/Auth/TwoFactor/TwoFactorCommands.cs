using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Auth.TwoFactor;

public sealed record EnableTwoFactorCommand : ICommand<ErrorOr<Success>>;

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

public sealed record DisableTwoFactorCommand(DisableTwoFactorRequest Request) : ICommand<ErrorOr<Success>>;

public sealed class DisableTwoFactorCommandHandler(
    ICurrentUserService currentUser,
    IUserAccountService accounts)
    : ICommandHandler<DisableTwoFactorCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(DisableTwoFactorCommand command, CancellationToken ct)
    {
        var userId = currentUser.User.Id;

        var check = await accounts.CheckPasswordAsync(userId, command.Request.Password, ct);

        if (check == PasswordCheckResult.LockedOut)
        {
            return Error.Forbidden(
                AuthErrorCodes.LockedOut,
                "This account is temporarily locked after too many failed attempts. Try again later.");
        }

        if (check == PasswordCheckResult.Failed)
        {
            return Error.Unauthorized(
                AuthErrorCodes.InvalidCredentials,
                "Email address or password is incorrect.");
        }

        await accounts.SetTwoFactorEnabledAsync(userId, false, ct);
        return Result.Success;
    }
}
