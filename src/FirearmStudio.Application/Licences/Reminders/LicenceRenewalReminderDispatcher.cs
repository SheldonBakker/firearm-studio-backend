using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;

namespace FirearmStudio.Application.Licences.Reminders;

internal sealed class LicenceRenewalReminderDispatcher(
    IKlaviyoClient klaviyo,
    KlaviyoSettings settings) : ILicenceRenewalReminderDispatcher
{
    public async Task DispatchAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<LicenceRenewalReminderPayload>(payloadJson, OutboxJson.Options)
            ?? throw new InvalidOperationException("Licence-renewal-reminder outbox payload deserialized to null.");

        var properties = new Dictionary<string, object?>
        {
            ["licence_number"] = payload.LicenceNumber,
            ["expires_on"] = payload.ExpiresOn.ToString("yyyy-MM-dd"),
            ["days_until_expiry"] = payload.DaysUntilExpiry,
            ["tier"] = payload.Tier,
            ["firearm_make"] = payload.FirearmMake,
            ["firearm_model"] = payload.FirearmModel,
            ["serial_number"] = payload.SerialNumber,
            ["company_id"] = payload.CompanyId,
            ["company_name"] = payload.CompanyName,
        };

        await klaviyo.TrackEventAsync(
            settings.LicenceRenewalMetricName,
            payload.Email,
            payload.CustomerName,
            properties,
            cancellationToken);
    }
}
