using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;

namespace FirearmStudio.Application.Invoices.GetInvoices;

public sealed record GetInvoicesQuery(
    int PageNumber,
    int PageSize
) : IQuery<ErrorOr<PaginatedResponse<InvoiceListItemDto>>>;
