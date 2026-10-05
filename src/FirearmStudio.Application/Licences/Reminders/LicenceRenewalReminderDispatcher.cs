using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Licences.Reminders;

internal sealed class LicenceRenewalReminderDispatcher(
    ITransactionalEmailSender emailSender,
    ILogger<LicenceRenewalReminderDispatcher> logger) : ILicenceRenewalReminderDispatcher
{
    public async Task DispatchAsync(Guid outboxMessageId, string payloadJson, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<LicenceRenewalReminderPayload>(payloadJson, OutboxJson.Options)
            ?? throw new InvalidOperationException("Licence-renewal-reminder outbox payload deserialized to null.");

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            logger.LogWarning(
                "Skipped licence-renewal-reminder email for licence {LicenceNumber}: customer has no email.",
                payload.LicenceNumber);
            return;
        }

        var company = CompanyEmailDetails.Empty with
        {
            Name = payload.CompanyName,
            Email = payload.CompanyEmail,
            Phone = payload.CompanyPhone,
        };

        var message = new LicenceRenewalReminderEmail(
            payload.Email,
            payload.CustomerName,
            payload.LicenceNumber,
            payload.ExpiresOn,
            payload.DaysUntilExpiry,
            payload.Tier,
            payload.FirearmMake,
            payload.FirearmModel,
            payload.SerialNumber,
            company)
        {
            IdempotencyKey = $"outbox:{outboxMessageId}",
        };

        await emailSender.SendAsync(message, cancellationToken);
    }
}
