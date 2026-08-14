namespace FirearmStudio.Application.Model.Options;

public sealed class JwtSettings
{
    public const string SectionName = nameof(JwtSettings);

    public required string Issuer { get; init; }

    public string Audience { get; init; } = "firearm-studio";

    public required string SigningKey { get; init; }

    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenDays { get; init; } = 14;

    public string[] ValidAlgorithms { get; init; } = ["HS256"];
}
