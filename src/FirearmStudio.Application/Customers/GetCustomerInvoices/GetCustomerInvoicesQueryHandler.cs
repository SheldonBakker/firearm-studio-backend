using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Customers.GetCustomerInvoices;

public sealed class GetCustomerInvoicesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetCustomerInvoicesQuery, ErrorOr<IReadOnlyList<CustomerInvoiceListItemDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<CustomerInvoiceListItemDto>>> Handle(
        GetCustomerInvoicesQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomerInvoiceListItemDto> invoices = await db.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == query.CustomerId)
            .Select(CustomerInvoiceListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(invoices);
    }
}
