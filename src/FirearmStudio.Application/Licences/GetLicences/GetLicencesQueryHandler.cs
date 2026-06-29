using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Licences.GetLicences;

public sealed class GetLicencesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetLicencesQuery, ErrorOr<PaginatedResponse<LicenceListItemDto>>>
{
    private const int MaxPageSize = 200;

    public async Task<ErrorOr<PaginatedResponse<LicenceListItemDto>>> Handle(
        GetLicencesQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

        var queryable = db.FirearmLicences.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.LicenceNumber))
        {
            var term = query.LicenceNumber.Trim().ToLower();
            queryable = queryable.Where(l => l.LicenceNumber.ToLower().Contains(term));
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(l => l.Status == query.Status.Value);
        }

        queryable = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? queryable.OrderByDescending(l => l.ExpiresOn).ThenBy(l => l.Id)
            : queryable.OrderBy(l => l.ExpiresOn).ThenBy(l => l.Id);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(LicenceListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<LicenceListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
