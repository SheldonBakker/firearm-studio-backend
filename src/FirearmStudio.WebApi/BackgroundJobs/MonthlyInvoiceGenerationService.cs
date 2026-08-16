using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Invoices.MonthlyInvoiceGeneration;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.BackgroundJobs;

public sealed class MonthlyInvoiceGenerationService(
    IServiceScopeFactory scopeFactory,
    ILogger<MonthlyInvoiceGenerationService> logger) : BackgroundService
{
    private const int ScheduledHour = 2;
    private bool _migrationsVerified;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var next = NextScheduledRunUtc(now);

            try
            {
                await Task.Delay(next - now, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Monthly invoice generation run failed.");
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        List<CompanyBilling> companies;
        using (var scope = scopeFactory.CreateScope())
        {
            if (!_migrationsVerified)
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                if (pending.Count > 0)
                {
                    var migrationNames = string.Join(", ", pending);
                    logger.LogError(
                        "Skipping monthly invoice generation: {Count} pending database migration(s): {Migrations}. " +
                        "Apply migrations and the job will resume on its next tick.",
                        pending.Count, migrationNames);
                    return;
                }

                _migrationsVerified = true;
            }

            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            companies = await db.Companies
                .AsNoTracking()
                .Where(company => company.IsActive && company.AutoBillingEnabled)
                .Select(company => new CompanyBilling(company.Id, company.VatNumber, company.DueDays))
                .ToListAsync(cancellationToken);
        }

        foreach (var company in companies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var scope = scopeFactory.CreateScope();
                var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                var generator = scope.ServiceProvider.GetRequiredService<IMonthlyInvoiceGenerator>();

                using (tenant.BeginCompanyScope(company.Id))
                {
                    var result = await generator.GenerateOutstandingAsync(
                        company.VatNumber, company.DueDays, cancellationToken);

                    if (result.InvoicesCreated > 0 && logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation(
                            "Generated {Created} invoice(s) ({Skipped} skipped) for company {CompanyId}.",
                            result.InvoicesCreated, result.InvoicesSkipped, company.Id);
                    }

                    if (result.MonthsFailed > 0 && logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning(
                            "{MonthsFailed} month(s) failed to save for company {CompanyId}; they will be retried on the next run.",
                            result.MonthsFailed, company.Id);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Monthly invoice generation failed for company {CompanyId}.", company.Id);
            }
        }
    }

    private static DateTime NextScheduledRunUtc(DateTime nowUtc)
    {
        var todayScheduled = nowUtc.Date.AddHours(ScheduledHour);
        return nowUtc < todayScheduled ? todayScheduled : todayScheduled.AddDays(1);
    }

    private sealed record CompanyBilling(Guid Id, string? VatNumber, int DueDays);
}
