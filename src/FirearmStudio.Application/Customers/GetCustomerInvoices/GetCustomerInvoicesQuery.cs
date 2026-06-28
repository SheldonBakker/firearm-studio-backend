using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Customers.GetCustomerInvoices;

public sealed record GetCustomerInvoicesQuery(Guid CustomerId)
    : IQuery<ErrorOr<IReadOnlyList<CustomerInvoiceListItemDto>>>;
