using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
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

        var queryable = db.Invoices
            .AsNoTracking()
            .OrderByDescending(i => i.InvoiceMonth)
            .ThenBy(i => i.Id);

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
