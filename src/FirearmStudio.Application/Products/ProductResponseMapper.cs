using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Products;

public static class ProductResponseMapper
{
    public static ProductResponse Map(ProductRow row, IFileStorage storage) =>
        ProductResponse.FromRow(
            row,
            row.ImageKey is null
                ? null
                : storage.GetPresignedReadUrl(row.ImageKey, ProductImageConstants.PresignLifetime));

    public static PublicProductResponse MapPublic(ProductRow row, IFileStorage storage) =>
        PublicProductResponse.FromRow(
            row,
            row.ImageKey is null
                ? null
                : storage.GetPresignedReadUrl(row.ImageKey, ProductImageConstants.PresignLifetime));
}
