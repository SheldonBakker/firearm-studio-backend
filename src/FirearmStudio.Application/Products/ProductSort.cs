using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Products;

public static class ProductSort
{
    public static IOrderedQueryable<Product> Apply(IQueryable<Product> source, string sortBy, string sortDir)
    {
        var desc = sortDir is not null && sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.ToLowerInvariant() switch
        {
            "price" => desc
                ? source.OrderByDescending(p => p.Price).ThenBy(p => p.Id)
                : source.OrderBy(p => p.Price).ThenBy(p => p.Id),
            "category" => desc
                ? source.OrderByDescending(p => p.Category).ThenBy(p => p.Id)
                : source.OrderBy(p => p.Category).ThenBy(p => p.Id),
            "stockquantity" => desc
                ? source.OrderByDescending(p => p.StockQuantity).ThenBy(p => p.Id)
                : source.OrderBy(p => p.StockQuantity).ThenBy(p => p.Id),
            "createdat" => desc
                ? source.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
                : source.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            _ => desc
                ? source.OrderByDescending(p => p.Name).ThenBy(p => p.Id)
                : source.OrderBy(p => p.Name).ThenBy(p => p.Id),
        };
    }
}
