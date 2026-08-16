using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Auth.Register;

public sealed class RegisterCommandHandler(
    IUserAccountService accounts,
    IOtpService otp,
    IOtpDispatcher dispatcher)
    : ICommandHandler<RegisterCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var address = command.Request.Email.Trim().ToLowerInvariant();
        var phone = string.IsNullOrEmpty(command.Request.PhoneNumber)
            ? null
            : command.Request.PhoneNumber.Trim();

        var existing = await accounts.FindByEmailAsync(address, cancellationToken);

        if (existing is not null)
        {
            if (!existing.EmailConfirmed)
            {
                await IssueAndSendAsync(existing.Id, address, existing.PhoneNumber, cancellationToken);
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

        if (phone is not null)
        {
            await accounts.SetPhoneNumberAsync(account.Id, phone, confirmed: false, cancellationToken);
        }

        await IssueAndSendAsync(account.Id, address, phone, cancellationToken);

        return Result.Success;
    }

    private async Task IssueAndSendAsync(Guid userId, string address, string? phone, CancellationToken ct)
    {
        var issued = await otp.IssueAsync(userId, OtpPurpose.EmailConfirmation, ct);

        if (issued.Status == OtpIssueStatus.Issued)
        {
            await dispatcher.SendAsync(
                new OtpRecipient(address, null, phone),
                OtpPurpose.EmailConfirmation,
                issued.Code!,
                OtpConstants.CodeLifetimeMinutes,
                ct);
        }
    }
}
