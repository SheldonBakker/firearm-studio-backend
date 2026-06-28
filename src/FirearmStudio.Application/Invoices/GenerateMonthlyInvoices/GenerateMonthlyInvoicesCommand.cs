using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Invoices.GenerateMonthlyInvoices;

public sealed record GenerateMonthlyInvoicesCommand(GenerateMonthlyInvoicesRequest Request)
    : ICommand<ErrorOr<GenerateMonthlyInvoicesResponse>>;
