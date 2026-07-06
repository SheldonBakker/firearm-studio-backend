namespace FirearmStudio.Application.Model.Options;

public sealed class KlaviyoSettings
{
    public const string SectionName = nameof(KlaviyoSettings);

    public string ApiKey { get; init; } = "";

    public string BaseUrl { get; init; } = "https://a.klaviyo.com";

    public string ApiRevision { get; init; } = "2024-10-15";

    public string ContactMetricName { get; init; } = "Contact Form Submitted";

    public string InvoiceSentMetricName { get; init; } = "Invoice Sent";

    public string BookingRequestedMetricName { get; init; } = "Booking Requested";

    public string? ContactListId { get; init; }
}
