using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Products.DeleteProduct;

public sealed class DeleteProductCommandHandler(
    IApplicationDbContext db,
    IFileStorage storage,
    ILogger<DeleteProductCommandHandler> logger)
    : ICommandHandler<DeleteProductCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);
        if (product is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Product not found.");
        }

        var imageKey = product.ImageKey;

        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);

        if (imageKey is not null)
        {
            await storage.TryDeleteAsync(imageKey, logger, cancellationToken);
        }

        return Result.Deleted;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "DeleteProductCommand.NotFound";
    }
}
