using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Infrastructure.Services;

public sealed class OtpDispatcher(
    IEmailSender email,
    IWhatsAppSender whatsApp,
    ILogger<OtpDispatcher> logger) : IOtpDispatcher
{
    public Task SendAsync(
        OtpRecipient recipient,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct) =>
        purpose == OtpPurpose.PhoneChange
            ? SendToWhatsAppOnlyAsync(recipient, purpose, code, expiresInMinutes, ct)
            : SendToEmailWithBestEffortWhatsAppAsync(recipient, purpose, code, expiresInMinutes, ct);

    private Task SendToWhatsAppOnlyAsync(
        OtpRecipient recipient,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(recipient.PhoneNumber))
        {
            throw new InvalidOperationException(
                $"A {purpose} code has no destination number.");
        }

        return whatsApp.SendOtpAsync(recipient.PhoneNumber, purpose, code, expiresInMinutes, ct);
    }

    private async Task SendToEmailWithBestEffortWhatsAppAsync(
        OtpRecipient recipient,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct)
    {
        await email.SendOtpAsync(recipient.Email, recipient.Name, purpose, code, expiresInMinutes, ct);

        if (string.IsNullOrEmpty(recipient.PhoneNumber))
        {
            return;
        }

        try
        {
            await whatsApp.SendOtpAsync(recipient.PhoneNumber, purpose, code, expiresInMinutes, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Best-effort WhatsApp OTP delivery failed for purpose {Purpose}.",
                purpose);
        }
    }
}
