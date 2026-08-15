using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Auth;
using FirearmStudio.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Users.UpdatePhone;

public sealed record UpdatePhoneCommand(UpdatePhoneRequest Request) : ICommand<ErrorOr<Success>>;

public sealed class UpdatePhoneCommandHandler(
    ICurrentUserService currentUser,
    IUserAccountService accounts,
    IOtpService otp,
    IOtpDispatcher dispatcher,
    ILogger<UpdatePhoneCommandHandler> logger)
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

        await otp.InvalidateAsync(userId, OtpPurpose.PhoneChange, ct);

        await accounts.SetPendingPhoneNumberAsync(userId, phone, ct);

        var issued = await otp.IssueAsync(userId, OtpPurpose.PhoneChange, ct);
        if (issued.Status != OtpIssueStatus.Issued)
        {
            return Error.Failure(
                AuthErrorCodes.ChallengeUnavailable,
                "Too many codes have been requested recently. Try again later.");
        }

        try
        {
            await dispatcher.SendAsync(
                new OtpRecipient(email, null, phone),
                OtpPurpose.PhoneChange,
                issued.Code!,
                CodeLifetimeMinutes,
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Delivery of a {Purpose} code failed.",
                OtpPurpose.PhoneChange);

            return Error.Failure(
                AuthErrorCodes.PhoneChannelUnavailable,
                "A verification code could not be sent to that number right now. Try again later.");
        }

        return Result.Success;
    }
}
