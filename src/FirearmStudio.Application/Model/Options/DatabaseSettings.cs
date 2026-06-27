namespace FirearmStudio.Application.Model.Options;

public sealed class DatabaseSettings
{
    public const string SectionName = nameof(DatabaseSettings);

    public required string ConnectionString { get; init; }
}
