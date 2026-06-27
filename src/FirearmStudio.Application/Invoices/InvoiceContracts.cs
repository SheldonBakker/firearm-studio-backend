using ErrorOr;

namespace FirearmStudio.Application.Invoices;

public sealed record GenerateMonthlyInvoicesRequest(DateOnly InvoiceMonth, decimal VatRate, int DueDays);

public sealed record GenerateMonthlyInvoicesResponse(int InvoicesCreated, int InvoicesSkipped);

public interface IInvoiceGenerationService
{
    Task<ErrorOr<GenerateMonthlyInvoicesResponse>> GenerateMonthlyAsync(
        GenerateMonthlyInvoicesRequest request, CancellationToken ct = default);
}
