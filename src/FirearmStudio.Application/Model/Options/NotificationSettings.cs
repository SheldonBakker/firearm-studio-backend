namespace FirearmStudio.Application.Model.Options;

public sealed class NotificationSettings
{
    public const string SectionName = nameof(NotificationSettings);

    public string PublicBaseUrl { get; init; } = "";
}
