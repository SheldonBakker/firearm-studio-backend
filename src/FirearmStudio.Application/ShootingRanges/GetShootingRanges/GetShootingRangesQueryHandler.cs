using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.ShootingRanges.GetShootingRanges;

public sealed class GetShootingRangesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetShootingRangesQuery, ErrorOr<PaginatedResponse<ShootingRangeListItemDto>>>
{
    private const int MaxPageSize = 200;

    public async Task<ErrorOr<PaginatedResponse<ShootingRangeListItemDto>>> Handle(
        GetShootingRangesQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

        var queryable = db.ShootingRanges.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.Name.Trim());
            queryable = queryable.Where(r => EF.Functions.ILike(r.Name, pattern));
        }

        if (query.IsActive.HasValue)
        {
            queryable = queryable.Where(r => r.IsActive == query.IsActive.Value);
        }

        var desc = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        queryable = desc
            ? queryable.OrderByDescending(r => r.Name).ThenBy(r => r.Id)
            : queryable.OrderBy(r => r.Name).ThenBy(r => r.Id);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ShootingRangeListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<ShootingRangeListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
