using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IUserAccountService accounts,
    IOtpService otp,
    IOtpDispatcher dispatcher)
    : ICommandHandler<ForgotPasswordCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var address = command.Request.Email.Trim().ToLowerInvariant();

        var account = await accounts.FindByEmailAsync(address, cancellationToken);

        if (account is not null)
        {
            var issued = await otp.IssueAsync(
                account.Id, OtpPurpose.PasswordReset, cancellationToken);

            if (issued.Status == OtpIssueStatus.Issued)
            {
                await dispatcher.SendAsync(
                    new OtpRecipient(
                        address,
                        null,
                        account.PhoneNumberConfirmed ? account.PhoneNumber : null),
                    OtpPurpose.PasswordReset,
                    issued.Code!,
                    OtpConstants.CodeLifetimeMinutes,
                    cancellationToken);
            }
        }

        return Result.Success;
    }
}
