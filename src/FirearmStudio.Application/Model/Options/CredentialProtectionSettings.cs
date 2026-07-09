namespace FirearmStudio.Application.Model.Options;

public sealed class CredentialProtectionSettings
{
    public const string SectionName = nameof(CredentialProtectionSettings);

    public string Key { get; init; } = "";
}
