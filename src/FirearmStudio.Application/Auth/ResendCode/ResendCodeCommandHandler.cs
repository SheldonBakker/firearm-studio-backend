using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Auth.ResendCode;

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

        var issued = await otp.IssueAsync(account.Id, purpose, cancellationToken);

        if (issued.Status == OtpIssueStatus.Issued)
        {
            await dispatcher.SendAsync(
                new OtpRecipient(address, null),
                purpose,
                issued.Code!,
                OtpConstants.CodeLifetimeMinutes,
                cancellationToken);
        }

        return Result.Success;
    }

    private static bool IsResendableAnonymously(OtpPurpose purpose) =>
        purpose is not OtpPurpose.TwoFactor;
}
