using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Auth;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Users.UpdatePhone;

public sealed record UpdatePhoneCommand(UpdatePhoneRequest Request) : ICommand<ErrorOr<Success>>;

public sealed class UpdatePhoneCommandHandler(
    ICurrentUserService currentUser,
    IUserAccountService accounts,
    IOtpService otp,
    IOtpDispatcher dispatcher)
    : ICommandHandler<UpdatePhoneCommand, ErrorOr<Success>>
{
    private const int CodeLifetimeMinutes = 15;

    public async Task<ErrorOr<Success>> Handle(UpdatePhoneCommand command, CancellationToken ct)
    {
        var userId = currentUser.User.Id;
        var phone = command.Request.PhoneNumber.Trim();

        var email = currentUser.User.Email;
        if (string.IsNullOrEmpty(email))
        {
            return Error.Unauthorized(
                AuthErrorCodes.InvalidCredentials,
                "Your account has no email address on file; a phone change cannot be confirmed.");
        }

        await accounts.SetPendingPhoneNumberAsync(userId, phone, ct);

        var issued = await otp.IssueAsync(userId, OtpPurpose.PhoneChange, ct);
        if (issued.Status == OtpIssueStatus.Issued)
        {
            await dispatcher.SendAsync(
                new OtpRecipient(email, null, phone),
                OtpPurpose.PhoneChange,
                issued.Code!,
                CodeLifetimeMinutes,
                ct);
        }

        return Result.Success;
    }
}
