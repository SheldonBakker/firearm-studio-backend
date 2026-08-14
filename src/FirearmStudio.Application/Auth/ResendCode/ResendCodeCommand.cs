using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Auth.ResendCode;

public sealed record ResendCodeCommand(ResendCodeRequest Request) : ICommand<ErrorOr<Success>>;

public sealed class ResendCodeCommandHandler(
    IUserAccountService accounts,
    IOtpService otp,
    IEmailSender email)
    : ICommandHandler<ResendCodeCommand, ErrorOr<Success>>
{
    private const int CodeLifetimeMinutes = 15;

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

        var address = command.Request.Email.Trim().ToLowerInvariant();
        var account = await accounts.FindByEmailAsync(address, cancellationToken);

        if (account is not null)
        {
            var issued = await otp.IssueAsync(account.Id, purpose, cancellationToken);

            if (issued.Status == OtpIssueStatus.Issued)
            {
                await email.SendOtpAsync(
                    address, null, purpose, issued.Code!, CodeLifetimeMinutes, cancellationToken);
            }
        }

        return Result.Success;
    }
}
