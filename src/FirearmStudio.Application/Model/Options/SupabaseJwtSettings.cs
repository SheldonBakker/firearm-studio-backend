namespace FirearmStudio.Application.Model.Options;

public sealed class SupabaseJwtSettings
{
    public const string SectionName = nameof(SupabaseJwtSettings);

    public required string Authority { get; init; }

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public string[] ValidAlgorithms { get; init; } = ["ES256"];

    public string? MetadataAddress { get; init; }
}
