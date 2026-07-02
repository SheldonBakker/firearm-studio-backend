namespace FirearmStudio.Application.Model.Options;

public sealed class KlaviyoSettings
{
    public const string SectionName = nameof(KlaviyoSettings);

    // Klaviyo private API key (pk_...).
    public required string ApiKey { get; init; }

    public string BaseUrl { get; init; } = "https://a.klaviyo.com";

    // Klaviyo API revision date sent via the `revision` header.
    public string ApiRevision { get; init; } = "2024-10-15";

    public string ContactMetricName { get; init; } = "Contact Form Submitted";
}
