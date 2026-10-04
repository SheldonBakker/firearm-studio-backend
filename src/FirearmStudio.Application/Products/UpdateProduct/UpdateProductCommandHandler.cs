using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FirearmStudio.Application.Products.UpdateProduct;

public sealed class UpdateProductCommandHandler(IApplicationDbContext db, IFileStorage storage)
    : ICommandHandler<UpdateProductCommand, ErrorOr<ProductResponse>>
{
    public async Task<ErrorOr<ProductResponse>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);
        if (product is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Product not found.");
        }

        var request = command.Request;
        request.Name.ApplyTo(v => product.Name = v.Trim());
        request.Description.ApplyTo(v => product.Description = v);
        request.Sku.ApplyTo(v => product.Sku = string.IsNullOrWhiteSpace(v) ? null : v.Trim());
        request.Price.ApplyTo(v => product.Price = v);
        request.CostPrice.ApplyTo(v => product.CostPrice = v);
        request.Category.ApplyTo(v => product.Category = string.IsNullOrWhiteSpace(v) ? null : v.Trim());
        request.StockQuantity.ApplyTo(v => product.StockQuantity = v);
        request.IsActive.ApplyTo(v => product.IsActive = v);

        if (request.Sku.IsSet && product.Sku is not null)
        {
            var normalized = product.Sku.ToLower();
            var conflict = await db.Products.AnyAsync(
                p => p.Id != command.Id && p.Sku != null && p.Sku.ToLower() == normalized, cancellationToken);
            if (conflict)
            {
                return Error.Conflict(ErrorCodes.SkuConflict, "A product with this SKU already exists.");
            }
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.GetBaseException() is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Error.Conflict(ErrorCodes.SkuConflict, "A product with this SKU already exists.");
        }

        return ProductResponseMapper.Map(ProductRow.FromEntity(product), storage);
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateProductCommand.NotFound";
        public const string SkuConflict = "UpdateProductCommand.SkuConflict";
    }
}
