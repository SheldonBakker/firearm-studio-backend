using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Auth.DisableTwoFactor;

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
