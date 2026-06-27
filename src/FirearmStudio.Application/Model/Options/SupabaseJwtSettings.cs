namespace FirearmStudio.Application.Model.Options;

public sealed class SupabaseJwtSettings
{
    public const string SectionName = nameof(SupabaseJwtSettings);

    public required string Authority { get; init; }

    public string? Issuer { get; init; }

    public string Audience { get; init; } = "authenticated";

    public string[] ValidAlgorithms { get; init; } = ["ES256"];

    public string? MetadataAddress { get; init; }

    public string EffectiveIssuer => string.IsNullOrWhiteSpace(Issuer) ? Authority : Issuer;
}
