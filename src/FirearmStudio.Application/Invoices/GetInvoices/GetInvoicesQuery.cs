using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Invoices.GetInvoices;

public sealed record GetInvoicesQuery : IQuery<ErrorOr<IReadOnlyList<InvoiceListItemDto>>>;
