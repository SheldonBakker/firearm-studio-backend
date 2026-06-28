namespace FirearmStudio.Application.Model.Options;

public sealed class ApiKeySettings
{
    public const string SectionName = nameof(ApiKeySettings);

    public const string DefaultHeaderName = "X-Api-Key";

    public required string Key { get; init; }

    public string HeaderName { get; init; } = DefaultHeaderName;
}
