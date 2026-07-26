using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
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
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.SerialNumber.Trim());
            queryable = queryable.Where(f => EF.Functions.ILike(f.SerialNumber, pattern));
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(f => f.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerName))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.CustomerName.Trim());
            queryable = queryable.Where(f =>
                (f.Customer!.FullName != null && EF.Functions.ILike(f.Customer.FullName, pattern)) ||
                (f.Customer!.CompanyName != null && EF.Functions.ILike(f.Customer.CompanyName, pattern)));
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
