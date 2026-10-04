using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Products;

public static class ProductImageKey
{
    public static string Build(Guid companyId, Guid productId, string contentType) =>
        $"companies/{companyId}/products/{productId}/{Guid.CreateVersion7()}.{Extension(contentType)}";

    public static string Extension(string contentType) => contentType.ToLowerInvariant() switch
    {
        ProductImageConstants.ContentTypeJpeg => "jpg",
        ProductImageConstants.ContentTypePng => "png",
        ProductImageConstants.ContentTypeWebp => "webp",
        _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Unsupported image content type."),
    };
}
