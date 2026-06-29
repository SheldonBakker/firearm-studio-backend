using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
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
            var term = query.Name.Trim().ToLower();
            queryable = queryable.Where(c =>
                (c.FullName != null && c.FullName.ToLower().Contains(term)) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var term = query.Email.Trim().ToLower();
            queryable = queryable.Where(c => c.Email != null && c.Email.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Phone))
        {
            var term = query.Phone.Trim().ToLower();
            queryable = queryable.Where(c => c.Phone != null && c.Phone.ToLower().Contains(term));
        }

        queryable = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? queryable.OrderByDescending(c => c.FullName).ThenBy(c => c.Id)
            : queryable.OrderBy(c => c.FullName).ThenBy(c => c.Id);

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
