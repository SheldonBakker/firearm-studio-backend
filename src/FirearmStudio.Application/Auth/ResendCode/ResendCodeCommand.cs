using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Auth.ResendCode;

public sealed record ResendCodeCommand(ResendCodeRequest Request) : ICommand<ErrorOr<Success>>;

public sealed class ResendCodeCommandHandler(
    IUserAccountService accounts,
    IOtpService otp,
    IOtpDispatcher dispatcher)
    : ICommandHandler<ResendCodeCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(
        ResendCodeCommand command,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OtpPurpose>(command.Request.Purpose, ignoreCase: true, out var purpose))
        {
            return Error.Validation(
                AuthErrorCodes.UnknownPurpose,
                "Unknown code purpose.");
        }

        if (!IsResendableAnonymously(purpose))
        {
            return Error.Validation(
                AuthErrorCodes.PurposeNotResendable,
                "Codes for that purpose cannot be resent from here.");
        }

        var address = command.Request.Email.Trim().ToLowerInvariant();
        var account = await accounts.FindByEmailAsync(address, cancellationToken);

        if (account is null)
        {
            return Result.Success;
        }

        string? destinationPhone;
        if (purpose == OtpPurpose.PhoneChange)
        {
            if (string.IsNullOrEmpty(account.PendingPhoneNumber))
            {
                return Error.Validation(
                    AuthErrorCodes.PhoneMissing,
                    "There is no phone change in progress to resend a code for.");
            }

            destinationPhone = account.PendingPhoneNumber;
        }
        else
        {
            destinationPhone = account.PhoneNumberConfirmed ? account.PhoneNumber : null;
        }

        var issued = await otp.IssueAsync(account.Id, purpose, cancellationToken);

        if (issued.Status == OtpIssueStatus.Issued)
        {
            await dispatcher.SendAsync(
                new OtpRecipient(address, null, destinationPhone),
                purpose,
                issued.Code!,
                OtpConstants.CodeLifetimeMinutes,
                cancellationToken);
        }

        return Result.Success;
    }

    private static bool IsResendableAnonymously(OtpPurpose purpose) =>
        purpose is not (OtpPurpose.TwoFactor or OtpPurpose.PhoneChange);
}
