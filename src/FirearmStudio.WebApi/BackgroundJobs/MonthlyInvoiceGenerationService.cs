using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Invoices.MonthlyInvoiceGeneration;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.BackgroundJobs;

public sealed class MonthlyInvoiceGenerationService(
    IServiceScopeFactory scopeFactory,
    ILogger<MonthlyInvoiceGenerationService> logger)
    : DailyJobBase(scopeFactory, logger)
{
    protected override int ScheduledHourUtc => 2;
    protected override void LogRunFailed(Exception ex) =>
        logger.LogError(ex, "Monthly invoice generation run failed.");

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        List<CompanyBilling> companies;
        using (var scope = ScopeFactory.CreateScope())
        {
            if (!await EnsureMigrationsVerifiedAsync(scope, "monthly invoice generation", cancellationToken))
            {
                return;
            }

            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            companies = await db.Companies
                .AsNoTracking()
                .Where(company => company.IsActive && company.AutoBillingEnabled)
                .Select(company => new CompanyBilling(company.Id, company.VatNumber, company.DueDays))
                .ToListAsync(cancellationToken);
        }

        await RunForAllCompaniesAsync(
            companies,
            static c => c.Id,
            async (scope, company, ct) =>
            {
                var generator = scope.ServiceProvider.GetRequiredService<IMonthlyInvoiceGenerator>();

                var result = await generator.GenerateOutstandingAsync(company.VatNumber, company.DueDays, ct);

                if (result.InvoicesCreated > 0 && Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation(
                        "Generated {Created} invoice(s) ({Skipped} skipped) for company {CompanyId}.",
                        result.InvoicesCreated, result.InvoicesSkipped, company.Id);
                }

                if (result.MonthsFailed > 0 && Logger.IsEnabled(LogLevel.Warning))
                {
                    Logger.LogWarning(
                        "{MonthsFailed} month(s) failed to save for company {CompanyId}; they will be retried on the next run.",
                        result.MonthsFailed, company.Id);
                }
            },
            (ex, id) => logger.LogError(ex, "Monthly invoice generation failed for company {CompanyId}.", id),
            cancellationToken);
    }

    private sealed record CompanyBilling(Guid Id, string? VatNumber, int DueDays);
}
