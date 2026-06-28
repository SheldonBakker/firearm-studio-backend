using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Invoices.GenerateMonthlyInvoices;

public sealed class GenerateMonthlyInvoicesCommandHandler(IInvoiceGenerationService invoiceGeneration)
    : ICommandHandler<GenerateMonthlyInvoicesCommand, ErrorOr<GenerateMonthlyInvoicesResponse>>
{
    public async Task<ErrorOr<GenerateMonthlyInvoicesResponse>> Handle(
        GenerateMonthlyInvoicesCommand command, CancellationToken cancellationToken) =>
        await invoiceGeneration.GenerateMonthlyAsync(command.Request, cancellationToken);
}
