using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FirearmStudio.Application.Products.CreateProduct;

public sealed class CreateProductCommandHandler(IApplicationDbContext db, IFileStorage storage)
    : ICommandHandler<CreateProductCommand, ErrorOr<ProductResponse>>
{
    public async Task<ErrorOr<ProductResponse>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim();
        if (sku is not null)
        {
            var normalized = sku.ToLower();
            var exists = await db.Products.AnyAsync(
                p => p.Sku != null && p.Sku.ToLower() == normalized, cancellationToken);
            if (exists)
            {
                return Error.Conflict(ErrorCodes.SkuConflict, "A product with this SKU already exists.");
            }
        }

        var category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Sku = sku,
            Price = request.Price,
            CostPrice = request.CostPrice,
            Category = category,
            StockQuantity = request.StockQuantity,
            IsActive = request.IsActive,
        };

        await db.Products.AddAsync(product, cancellationToken);
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
        public const string SkuConflict = "CreateProductCommand.SkuConflict";
    }
}
