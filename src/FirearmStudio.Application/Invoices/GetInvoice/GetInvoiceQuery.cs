using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Invoices.GetInvoice;

public sealed record GetInvoiceQuery(Guid Id) : IQuery<ErrorOr<InvoiceDetailDto>>;
