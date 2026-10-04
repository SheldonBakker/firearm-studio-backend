using System.Linq.Expressions;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Products;

public sealed record CreateProductRequest(
    string Name,
    string? Description,
    string? Sku,
    decimal Price,
    decimal? CostPrice,
    string? Category,
    int StockQuantity = 0,
    bool IsActive = true);

public sealed record UpdateProductRequest(
    Optional<string> Name,
    Optional<string?> Description,
    Optional<string?> Sku,
    Optional<decimal> Price,
    Optional<decimal?> CostPrice,
    Optional<string?> Category,
    Optional<int> StockQuantity,
    Optional<bool> IsActive);

public sealed record ProductRow(
    Guid Id,
    string Name,
    string? Description,
    string? Sku,
    decimal Price,
    decimal? CostPrice,
    string? Category,
    int StockQuantity,
    string? ImageKey,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static Expression<Func<Product, ProductRow>> QueryProjection => p => new ProductRow(
        p.Id, p.Name, p.Description, p.Sku, p.Price, p.CostPrice, p.Category,
        p.StockQuantity, p.ImageKey, p.IsActive, p.CreatedAt, p.UpdatedAt);

    public static ProductRow FromEntity(Product p) => new(
        p.Id, p.Name, p.Description, p.Sku, p.Price, p.CostPrice, p.Category,
        p.StockQuantity, p.ImageKey, p.IsActive, p.CreatedAt, p.UpdatedAt);
}

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Sku,
    decimal Price,
    decimal? CostPrice,
    string? Category,
    int StockQuantity,
    string? ImageUrl,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static ProductResponse FromRow(ProductRow row, string? imageUrl) => new(
        row.Id, row.Name, row.Description, row.Sku, row.Price, row.CostPrice, row.Category,
        row.StockQuantity, imageUrl, row.IsActive, row.CreatedAt, row.UpdatedAt);
}
