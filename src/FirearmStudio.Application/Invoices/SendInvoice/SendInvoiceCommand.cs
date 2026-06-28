using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Invoices.SendInvoice;

public sealed record SendInvoiceCommand(Guid Id) : ICommand<ErrorOr<Updated>>;
