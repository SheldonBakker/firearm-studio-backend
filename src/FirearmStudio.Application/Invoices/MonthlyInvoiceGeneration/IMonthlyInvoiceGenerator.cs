namespace FirearmStudio.Application.Invoices.MonthlyInvoiceGeneration;

public interface IMonthlyInvoiceGenerator
{
    Task<MonthlyInvoiceGenerationResult> GenerateOutstandingAsync(
        string? vatNumber,
        int dueDays,
        CancellationToken cancellationToken);
}

public sealed record MonthlyInvoiceGenerationResult(int InvoicesCreated, int InvoicesSkipped, int MonthsFailed);
