using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Firearms.GetFirearms;

public sealed class GetFirearmsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetFirearmsQuery, ErrorOr<PaginatedResponse<FirearmResponse>>>
{
    private const int MaxPageSize = 200;

    public async Task<ErrorOr<PaginatedResponse<FirearmResponse>>> Handle(GetFirearmsQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

        var queryable = db.Firearms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SerialNumber))
        {
            var term = query.SerialNumber.Trim().ToLower();
            queryable = queryable.Where(f => f.SerialNumber.ToLower().Contains(term));
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(f => f.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerName))
        {
            var term = query.CustomerName.Trim().ToLower();
            queryable = queryable.Where(f =>
                (f.Customer!.FullName != null && f.Customer.FullName.ToLower().Contains(term)) ||
                (f.Customer!.CompanyName != null && f.Customer.CompanyName.ToLower().Contains(term)));
        }

        queryable = queryable
            .OrderBy(f => f.SerialNumber)
            .ThenBy(f => f.Id);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(FirearmResponse.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<FirearmResponse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
