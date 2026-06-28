using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Invoices.CancelInvoice;

public sealed record CancelInvoiceCommand(Guid Id) : ICommand<ErrorOr<Updated>>;
