using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Customers.GetCustomers;

public sealed class GetCustomersQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetCustomersQuery, ErrorOr<PaginatedResponse<CustomerListItemDto>>>
{
    private const int MaxPageSize = 200;

    public async Task<ErrorOr<PaginatedResponse<CustomerListItemDto>>> Handle(
        GetCustomersQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

        var queryable = db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.Name.Trim());
            queryable = queryable.Where(c =>
                (c.FullName != null && EF.Functions.ILike(c.FullName, pattern)) ||
                (c.CompanyName != null && EF.Functions.ILike(c.CompanyName, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.Email.Trim());
            queryable = queryable.Where(c => c.Email != null && EF.Functions.ILike(c.Email, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Phone))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.Phone.Trim());
            queryable = queryable.Where(c => c.Phone != null && EF.Functions.ILike(c.Phone, pattern));
        }

        queryable = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? queryable.OrderByDescending(c => c.FullName ?? c.CompanyName).ThenBy(c => c.Id)
            : queryable.OrderBy(c => c.FullName ?? c.CompanyName).ThenBy(c => c.Id);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(CustomerListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<CustomerListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
