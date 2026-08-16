using System.Linq.Expressions;
using ErrorOr;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Extensions;

internal static class QueryableExtensions
{
    internal const int DefaultPageSize = 20;
    internal const int MaxPageSize = 200;

    internal static int ClampPageNumber(int pageNumber) => pageNumber < 1 ? 1 : pageNumber;

    internal static int ClampPageSize(int pageSize) =>
        pageSize is < 1 or > MaxPageSize ? DefaultPageSize : pageSize;

    internal static async Task<PaginatedResponse<TDto>> ToPaginatedAsync<TEntity, TDto>(
        this IQueryable<TEntity> source,
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, TDto>> projection,
        CancellationToken ct)
        where TDto : class
    {
        var page = ClampPageNumber(pageNumber);
        var size = ClampPageSize(pageSize);

        var totalCount = await source.CountAsync(ct);

        var items = await source
            .Skip((page - 1) * size)
            .Take(size)
            .Select(projection)
            .ToListAsync(ct);

        return new PaginatedResponse<TDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = totalCount,
        };
    }

    internal static async Task<ErrorOr<TDto>> FirstOrNotFoundAsync<TEntity, TDto>(
        this IQueryable<TEntity> source,
        Expression<Func<TEntity, TDto>> projection,
        string errorCode,
        string message,
        CancellationToken ct)
        where TDto : class
    {
        var result = await source.Select(projection).FirstOrDefaultAsync(ct);
        return result is null ? Error.NotFound(errorCode, message) : result;
    }
}
