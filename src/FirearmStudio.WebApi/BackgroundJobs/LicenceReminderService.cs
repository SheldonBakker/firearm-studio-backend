using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Licences.Reminders;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.BackgroundJobs;

public sealed class LicenceReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<LicenceReminderService> logger)
    : DailyJobBase(scopeFactory, logger)
{
    protected override int ScheduledHourUtc => 3;
    protected override void LogRunFailed(Exception ex) =>
        logger.LogError(ex, "Licence reminder run failed.");

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        List<LicenceReminderCompany> companies;
        using (var scope = ScopeFactory.CreateScope())
        {
            if (!await EnsureMigrationsVerifiedAsync(scope, "licence reminders", cancellationToken))
            {
                return;
            }

            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            companies = await db.Companies
                .AsNoTracking()
                .Where(company => company.IsActive)
                .Select(company => new LicenceReminderCompany(company.Id, company.Name))
                .ToListAsync(cancellationToken);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await RunForAllCompaniesAsync(
            companies,
            static c => c.Id,
            async (scope, company, ct) =>
            {
                var generator = scope.ServiceProvider.GetRequiredService<ILicenceReminderGenerator>();

                var result = await generator.GenerateAsync(company, today, ct);

                if ((result.RemindersQueued > 0 || result.StatusesUpdated > 0)
                    && Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation(
                        "Licence reminders for company {CompanyId}: {Queued} queued, {Statuses} status update(s), {Skipped} skipped (no email).",
                        company.Id, result.RemindersQueued, result.StatusesUpdated, result.SkippedNoEmail);
                }
            },
            (ex, id) => logger.LogError(ex, "Licence reminder generation failed for company {CompanyId}.", id),
            cancellationToken);
    }
}
