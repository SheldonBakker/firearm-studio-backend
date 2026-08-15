namespace FirearmStudio.Application.Model.Options;

public sealed class WahaSettings
{
    public const string SectionName = nameof(WahaSettings);

    public string BaseUrl { get; init; } = "";

    public string SessionId { get; init; } = "";

    public string ApiKey { get; init; } = "";

    public bool Enabled { get; init; }

    public int TimeoutSeconds { get; init; } = 10;
}
