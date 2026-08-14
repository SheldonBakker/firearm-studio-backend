using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Auth.Register;

public sealed record RegisterCommand(RegisterRequest Request) : ICommand<ErrorOr<Success>>;

public sealed class RegisterCommandHandler(
    IUserAccountService accounts,
    IOtpService otp,
    IEmailSender email)
    : ICommandHandler<RegisterCommand, ErrorOr<Success>>
{
    private const int CodeLifetimeMinutes = 15;

    public async Task<ErrorOr<Success>> Handle(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var address = command.Request.Email.Trim().ToLowerInvariant();

        var existing = await accounts.FindByEmailAsync(address, cancellationToken);

        if (existing is not null)
        {
            if (!existing.EmailConfirmed)
            {
                await IssueAndSendAsync(existing.Id, address, cancellationToken);
            }

            return Result.Success;
        }

        var (account, errors) = await accounts.CreateAsync(
            address, command.Request.Password, cancellationToken);

        if (account is null)
        {
            return Error.Validation(
                AuthErrorCodes.RegistrationFailed,
                string.Join(" ", errors));
        }

        await IssueAndSendAsync(account.Id, address, cancellationToken);

        return Result.Success;
    }

    private async Task IssueAndSendAsync(Guid userId, string address, CancellationToken ct)
    {
        var issued = await otp.IssueAsync(userId, OtpPurpose.EmailConfirmation, ct);

        if (issued.Status == OtpIssueStatus.Issued)
        {
            await email.SendOtpAsync(
                address, null, OtpPurpose.EmailConfirmation, issued.Code!, CodeLifetimeMinutes, ct);
        }
    }
}
