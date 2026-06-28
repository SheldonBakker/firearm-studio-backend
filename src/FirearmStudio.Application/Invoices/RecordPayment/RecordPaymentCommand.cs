using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Invoices.RecordPayment;

public sealed record RecordPaymentCommand(Guid Id, RecordPaymentRequest Request) : ICommand<ErrorOr<Updated>>;
