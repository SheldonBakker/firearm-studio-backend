using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Invoices.GetInvoices;

public sealed class GetInvoicesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetInvoicesQuery, ErrorOr<PaginatedResponse<InvoiceListItemDto>>>
{
    private const int MaxPageSize = 200;

    public async Task<ErrorOr<PaginatedResponse<InvoiceListItemDto>>> Handle(
        GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

        var queryable = db.Invoices.AsNoTracking();

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(i => i.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.InvoiceNumber))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.InvoiceNumber.Trim());
            queryable = queryable.Where(i => EF.Functions.ILike(i.InvoiceNumber, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerName))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.CustomerName.Trim());
            queryable = queryable.Where(i =>
                (i.Customer!.FullName != null && EF.Functions.ILike(i.Customer.FullName, pattern)) ||
                (i.Customer!.CompanyName != null && EF.Functions.ILike(i.Customer.CompanyName, pattern)));
        }

        var desc = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        queryable = query.SortBy.ToLowerInvariant() switch
        {
            "total" => desc
                ? queryable.OrderByDescending(i => i.Total).ThenBy(i => i.Id)
                : queryable.OrderBy(i => i.Total).ThenBy(i => i.Id),
            "dueon" => desc
                ? queryable.OrderByDescending(i => i.DueOn).ThenBy(i => i.Id)
                : queryable.OrderBy(i => i.DueOn).ThenBy(i => i.Id),
            "invoicenumber" => desc
                ? queryable.OrderByDescending(i => i.InvoiceNumber).ThenBy(i => i.Id)
                : queryable.OrderBy(i => i.InvoiceNumber).ThenBy(i => i.Id),
            "status" => desc
                ? queryable.OrderByDescending(i => i.Status).ThenBy(i => i.Id)
                : queryable.OrderBy(i => i.Status).ThenBy(i => i.Id),
            _ => desc
                ? queryable.OrderByDescending(i => i.InvoiceMonth).ThenBy(i => i.Id)
                : queryable.OrderBy(i => i.InvoiceMonth).ThenBy(i => i.Id),
        };

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(InvoiceListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<InvoiceListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
