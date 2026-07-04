using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Invoices.GetInvoices;

public sealed record GetInvoicesQuery(
    int PageNumber,
    int PageSize,
    string SortBy,
    string SortOrder,
    InvoiceStatus? Status,
    string? InvoiceNumber,
    string? CustomerName
) : IQuery<ErrorOr<PaginatedResponse<InvoiceListItemDto>>>;
