using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Invoices.GetInvoices;

public sealed class GetInvoicesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetInvoicesQuery, ErrorOr<IReadOnlyList<InvoiceListItemDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<InvoiceListItemDto>>> Handle(
        GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<InvoiceListItemDto> invoices = await db.Invoices
            .AsNoTracking()
            .OrderByDescending(i => i.InvoiceMonth)
            .Select(InvoiceListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(invoices);
    }
}
