using FirearmStudio.Application.Extensions;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Products;

public static class ProductFilters
{
    public static IQueryable<Product> ApplySearch(this IQueryable<Product> source, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return source;
        }

        var pattern = SearchPatternHelper.ToILikeContainsPattern(search.Trim());
        return source.Where(p =>
            EF.Functions.ILike(p.Name, pattern) ||
            (p.Sku != null && EF.Functions.ILike(p.Sku, pattern)));
    }

    public static IQueryable<Product> ApplyCategory(this IQueryable<Product> source, string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return source;
        }

        var pattern = SearchPatternHelper.ToILikeExactPattern(category.Trim());
        return source.Where(p => p.Category != null && EF.Functions.ILike(p.Category, pattern));
    }
}
