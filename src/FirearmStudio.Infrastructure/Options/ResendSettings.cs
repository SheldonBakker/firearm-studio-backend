namespace FirearmStudio.Infrastructure.Options;

public sealed class ResendSettings
{
    public const string SectionName = nameof(ResendSettings);
    public string ApiKey { get; init; } = "";
    public string BaseUrl { get; init; } = "https://api.resend.com";
    public string FromAddress { get; init; } = "";
    public string FromName { get; init; } = "Firearm Studio";
    public int TimeoutSeconds { get; init; } = 10;
    public string ContactInboxEmail { get; init; } = "";
    public string ContactSegmentId { get; init; } = "";
}
