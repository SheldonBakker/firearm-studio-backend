using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Infrastructure.Services;

public sealed class OtpDispatcher(IEmailSender email) : IOtpDispatcher
{
    public Task SendAsync(
        OtpRecipient recipient,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct) =>
        email.SendOtpAsync(recipient.Email, recipient.Name, purpose, code, expiresInMinutes, ct);
}
