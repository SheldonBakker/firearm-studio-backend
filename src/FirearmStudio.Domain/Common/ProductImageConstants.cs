namespace FirearmStudio.Domain.Common;

public static class ProductImageConstants
{
    public const long MaxImageBytes = 5 * 1024 * 1024;

    public const int PresignLifetimeMinutes = 60;

    public static readonly TimeSpan PresignLifetime = TimeSpan.FromMinutes(PresignLifetimeMinutes);

    public const string ContentTypeJpeg = "image/jpeg";
    public const string ContentTypePng = "image/png";
    public const string ContentTypeWebp = "image/webp";

    public static readonly IReadOnlyList<string> AllowedContentTypes =
        [ContentTypeJpeg, ContentTypePng, ContentTypeWebp];
}
