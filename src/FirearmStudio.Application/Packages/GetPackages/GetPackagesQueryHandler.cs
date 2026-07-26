using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Packages.GetPackages;

public sealed class GetPackagesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetPackagesQuery, ErrorOr<PaginatedResponse<PackageListItemDto>>>
{
    private const int MaxPageSize = 200;

    public async Task<ErrorOr<PaginatedResponse<PackageListItemDto>>> Handle(
        GetPackagesQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

        var queryable = db.Packages.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.Name.Trim());
            queryable = queryable.Where(p => EF.Functions.ILike(p.Name, pattern));
        }

        if (query.IsActive.HasValue)
        {
            queryable = queryable.Where(p => p.IsActive == query.IsActive.Value);
        }

        var desc = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        queryable = query.SortBy.ToLowerInvariant() switch
        {
            "price" => desc
                ? queryable.OrderByDescending(p => p.Price).ThenBy(p => p.Id)
                : queryable.OrderBy(p => p.Price).ThenBy(p => p.Id),
            "duration" => desc
                ? queryable.OrderByDescending(p => p.DurationMinutes).ThenBy(p => p.Id)
                : queryable.OrderBy(p => p.DurationMinutes).ThenBy(p => p.Id),
            _ => desc
                ? queryable.OrderByDescending(p => p.Name).ThenBy(p => p.Id)
                : queryable.OrderBy(p => p.Name).ThenBy(p => p.Id),
        };

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(PackageListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<PackageListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
