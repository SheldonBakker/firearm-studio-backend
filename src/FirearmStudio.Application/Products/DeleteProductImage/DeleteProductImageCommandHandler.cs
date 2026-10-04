using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Products.DeleteProductImage;

public sealed class DeleteProductImageCommandHandler(
    IApplicationDbContext db,
    IFileStorage storage,
    ILogger<DeleteProductImageCommandHandler> logger)
    : ICommandHandler<DeleteProductImageCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeleteProductImageCommand command, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);
        if (product is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Product not found.");
        }

        if (product.ImageKey is null)
        {
            return Result.Deleted;
        }

        var oldKey = product.ImageKey;
        product.ImageKey = null;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await storage.DeleteAsync(oldKey, cancellationToken);
        }
        catch (FileStorageException ex)
        {
            logger.LogWarning(ex, "Failed to delete product image object {OldKey}.", oldKey);
        }

        return Result.Deleted;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "DeleteProductImageCommand.NotFound";
    }
}
