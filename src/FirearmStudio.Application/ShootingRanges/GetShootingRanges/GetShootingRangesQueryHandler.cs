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
    public async Task<ErrorOr<PaginatedResponse<ShootingRangeListItemDto>>> Handle(
        GetShootingRangesQuery query, CancellationToken cancellationToken)
    {
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

        return await queryable.ToPaginatedAsync(
            query.PageNumber, query.PageSize, ShootingRangeListItemDto.QueryProjection, cancellationToken);
    }
}
