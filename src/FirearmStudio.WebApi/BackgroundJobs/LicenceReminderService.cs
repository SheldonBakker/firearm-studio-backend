using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Licences.Reminders;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.BackgroundJobs;

public sealed class LicenceReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<LicenceReminderService> logger) : BackgroundService
{
    private const int ScheduledHour = 3;
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
                logger.LogError(ex, "Licence reminder run failed.");
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        List<LicenceReminderCompany> companies;
        using (var scope = scopeFactory.CreateScope())
        {
            if (!_migrationsVerified)
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                if (pending.Count > 0)
                {
                    logger.LogError(
                        "Skipping licence reminders: {Count} pending database migration(s): {Migrations}. " +
                        "Apply migrations and the job will resume on its next tick.",
                        pending.Count, string.Join(", ", pending));
                    return;
                }

                _migrationsVerified = true;
            }

            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            companies = await db.Companies
                .AsNoTracking()
                .Where(company => company.IsActive)
                .Select(company => new LicenceReminderCompany(company.Id, company.Name))
                .ToListAsync(cancellationToken);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var company in companies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var scope = scopeFactory.CreateScope();
                var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                var generator = scope.ServiceProvider.GetRequiredService<ILicenceReminderGenerator>();

                using (tenant.BeginCompanyScope(company.Id))
                {
                    var result = await generator.GenerateAsync(company, today, cancellationToken);

                    if ((result.RemindersQueued > 0 || result.StatusesUpdated > 0)
                        && logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation(
                            "Licence reminders for company {CompanyId}: {Queued} queued, {Statuses} status update(s), {Skipped} skipped (no email).",
                            company.Id, result.RemindersQueued, result.StatusesUpdated, result.SkippedNoEmail);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Licence reminder generation failed for company {CompanyId}.", company.Id);
            }
        }
    }

    private static DateTime NextScheduledRunUtc(DateTime nowUtc)
    {
        var todayScheduled = nowUtc.Date.AddHours(ScheduledHour);
        return nowUtc < todayScheduled ? todayScheduled : todayScheduled.AddDays(1);
    }
}
