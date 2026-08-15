using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Infrastructure.Services;

public sealed class OtpDispatcher(
    IEmailSender email,
    IWhatsAppSender whatsApp,
    ILogger<OtpDispatcher> logger) : IOtpDispatcher
{
    public async Task SendAsync(
        OtpRecipient recipient,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct)
    {
        // Email is the required channel for every purpose; a failure propagates.
        await email.SendOtpAsync(recipient.Email, recipient.Name, purpose, code, expiresInMinutes, ct);

        // WhatsApp is additive/best-effort for every purpose: skipped without a number,
        // and never allowed to fail the operation.
        if (!string.IsNullOrEmpty(recipient.PhoneNumber))
        {
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
}
