using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Products.GetProducts;

public sealed class GetProductsQueryHandler(IApplicationDbContext db, IFileStorage storage)
    : IQueryHandler<GetProductsQuery, ErrorOr<PaginatedResponse<ProductResponse>>>
{
    public async Task<ErrorOr<PaginatedResponse<ProductResponse>>> Handle(
        GetProductsQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.Search.Trim());
            queryable = queryable.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                (p.Sku != null && EF.Functions.ILike(p.Sku, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var pattern = SearchPatternHelper.ToILikeExactPattern(query.Category.Trim());
            queryable = queryable.Where(p => p.Category != null && EF.Functions.ILike(p.Category, pattern));
        }

        if (query.IsActive.HasValue)
        {
            queryable = queryable.Where(p => p.IsActive == query.IsActive.Value);
        }

        if (query.MinPrice.HasValue)
        {
            queryable = queryable.Where(p => p.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            queryable = queryable.Where(p => p.Price <= query.MaxPrice.Value);
        }

        if (query.MinStock.HasValue)
        {
            queryable = queryable.Where(p => p.StockQuantity >= query.MinStock.Value);
        }

        if (query.MaxStock.HasValue)
        {
            queryable = queryable.Where(p => p.StockQuantity <= query.MaxStock.Value);
        }

        var ordered = ProductSort.Apply(queryable, query.SortBy, query.SortDir);

        var page = await ordered.ToPaginatedAsync(
            query.PageNumber, query.PageSize, ProductRow.QueryProjection, cancellationToken);

        var items = page.Items.Select(row => ProductResponseMapper.Map(row, storage)).ToList();

        return new PaginatedResponse<ProductResponse>
        {
            Items = items,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }
}
