using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Common;
using FirearmStudio.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Products.UploadProductImage;

public sealed class UploadProductImageCommandHandler(
    IApplicationDbContext db,
    IFileStorage storage,
    ILogger<UploadProductImageCommandHandler> logger)
    : ICommandHandler<UploadProductImageCommand, ErrorOr<ProductResponse>>
{
    public async Task<ErrorOr<ProductResponse>> Handle(UploadProductImageCommand command, CancellationToken cancellationToken)
    {
        if (command.Length <= 0 || command.Length > ProductImageConstants.MaxImageBytes)
        {
            return Error.Validation(ErrorCodes.TooLarge, "Image must be between 1 byte and 5 MB.");
        }

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);
        if (product is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Product not found.");
        }

        using var buffer = new MemoryStream(checked((int)command.Length));
        await command.Content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length > ProductImageConstants.MaxImageBytes)
        {
            return Error.Validation(ErrorCodes.TooLarge, "Image must be between 1 byte and 5 MB.");
        }

        var detected = ImageContentTypeDetector.Detect(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
        var declared = NormalizeDeclaredContentType(command.ContentType);

        if (detected is null ||
            !ProductImageConstants.AllowedContentTypes.Contains(detected) ||
            !string.Equals(detected, declared, StringComparison.Ordinal))
        {
            return Error.Validation(ErrorCodes.InvalidImage, "Unsupported or mismatched image format.");
        }

        buffer.Position = 0;

        var newKey = ProductImageKey.Build(product.CompanyId, product.Id, detected);

        try
        {
            await storage.UploadAsync(newKey, buffer, detected, cancellationToken);
        }
        catch (FileStorageException)
        {
            return Error.Custom(
                UpstreamErrorTypes.UpstreamFailure,
                ErrorCodes.StorageUnavailable,
                "Image storage is temporarily unavailable.");
        }

        var oldKey = product.ImageKey;
        product.ImageKey = newKey;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.TryDeleteAsync(newKey, logger, CancellationToken.None);
            throw;
        }

        if (oldKey is not null && oldKey != newKey)
        {
            await storage.TryDeleteAsync(oldKey, logger, cancellationToken);
        }

        return ProductResponseMapper.Map(ProductRow.FromEntity(product), storage);
    }

    private static string? NormalizeDeclaredContentType(string? contentType)
    {
        var lowered = contentType?.ToLowerInvariant();
        return lowered == "image/jpg" ? ProductImageConstants.ContentTypeJpeg : lowered;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UploadProductImageCommand.NotFound";
        public const string InvalidImage = "UploadProductImageCommand.InvalidImage";
        public const string TooLarge = "UploadProductImageCommand.TooLarge";
        public const string StorageUnavailable = "UploadProductImageCommand.StorageUnavailable";
    }
}
