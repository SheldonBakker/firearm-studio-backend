namespace FirearmStudio.Infrastructure.Options;

public sealed class KlaviyoSettings
{
    public const string SectionName = nameof(KlaviyoSettings);

    public string ApiKey { get; init; } = "";

    public string BaseUrl { get; init; } = "https://a.klaviyo.com";

    public string ApiRevision { get; init; } = "2024-10-15";
}
