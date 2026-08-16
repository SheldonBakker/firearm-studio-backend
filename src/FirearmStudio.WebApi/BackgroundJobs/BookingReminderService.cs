using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Bookings;
using FirearmStudio.Application.Common;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.BackgroundJobs;

public sealed class BookingReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingReminderService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private bool _migrationsVerified;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
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
                logger.LogError(ex, "Booking reminder run failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        List<Guid> companyIds;
        using (var scope = scopeFactory.CreateScope())
        {
            if (!_migrationsVerified)
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                if (pending.Count > 0)
                {
                    logger.LogError(
                        "Skipping booking reminders: {Count} pending database migration(s): {Migrations}. " +
                        "Apply migrations and the job will resume on its next tick.",
                        pending.Count, string.Join(", ", pending));
                    return;
                }

                _migrationsVerified = true;
            }

            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            companyIds = await db.Companies
                .AsNoTracking()
                .Where(company => company.IsActive)
                .Select(company => company.Id)
                .ToListAsync(cancellationToken);
        }

        var nowUtc = DateTime.UtcNow;
        var todayLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, SouthAfricaTimeZone.Instance));
        var fromDate = todayLocal.AddDays(-1);
        var toDate = todayLocal.AddDays(1);

        foreach (var companyId in companyIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var scope = scopeFactory.CreateScope();
                var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var lifecycleOutbox = scope.ServiceProvider.GetRequiredService<IBookingLifecycleOutbox>();
                var notificationSettings = scope.ServiceProvider.GetRequiredService<NotificationSettings>();

                using (tenant.BeginCompanyScope(companyId))
                {
                    var result = await RunForCompanyAsync(
                        db, lifecycleOutbox, notificationSettings, companyId, fromDate, toDate, nowUtc, cancellationToken);

                    if ((result.Queued > 0 || result.SkippedNoEmail > 0 || result.SkippedMissingRange > 0)
                        && logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation(
                            "Booking reminders for company {CompanyId}: {Queued} queued, {SkippedNoEmail} skipped " +
                            "(no email), {SkippedMissingRange} skipped (missing range).",
                            companyId, result.Queued, result.SkippedNoEmail, result.SkippedMissingRange);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Booking reminder generation failed for company {CompanyId}.", companyId);
            }
        }
    }

    private async Task<BookingReminderRunResult> RunForCompanyAsync(
        IApplicationDbContext db,
        IBookingLifecycleOutbox lifecycleOutbox,
        NotificationSettings notificationSettings,
        Guid companyId,
        DateOnly fromDate,
        DateOnly toDate,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var bookings = await db.Bookings
            .Where(b => b.Status == BookingStatus.Confirmed
                && b.ReminderSentAt == null
                && b.BookingDate >= fromDate
                && b.BookingDate <= toDate)
            .ToListAsync(cancellationToken);

        var dueBookings = bookings
            .Where(b => BookingReminderPlanner.IsReminderDue(nowUtc, b.BookingDate, b.StartTime))
            .ToList();

        if (dueBookings.Count == 0)
        {
            return new BookingReminderRunResult(0, 0, 0);
        }

        var company = await db.Companies
            .AsNoTracking()
            .FirstAsync(c => c.Id == companyId, cancellationToken);

        var rangeIds = dueBookings.Select(b => b.ShootingRangeId).Distinct().ToList();
        var rangeNames = await db.ShootingRanges
            .AsNoTracking()
            .Where(r => rangeIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var customerIds = dueBookings.Select(b => b.CustomerId).Distinct().ToList();
        var customers = await db.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.Email,
                Name = c.CustomerType == CustomerType.Company ? c.CompanyName : c.FullName,
            })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var invoiceIds = dueBookings
            .Where(b => b.InvoiceId is not null)
            .Select(b => b.InvoiceId!.Value)
            .Distinct()
            .ToList();
        var invoiceNumbers = await db.Invoices
            .AsNoTracking()
            .Where(i => invoiceIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.InvoiceNumber, cancellationToken);

        var queued = 0;
        var skippedNoEmail = 0;
        var skippedMissingRange = 0;

        foreach (var booking in dueBookings)
        {
            if (IsRangeMissing(rangeNames, booking.ShootingRangeId))
            {
                skippedMissingRange++;
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(
                        "Skipped BookingReminder event for booking {BookingNumber}: shooting range " +
                        "{ShootingRangeId} not found. ReminderSentAt left unset so this retries once fixed.",
                        booking.BookingNumber, booking.ShootingRangeId);
                }

                continue;
            }

            var rangeName = rangeNames[booking.ShootingRangeId];
            booking.ReminderSentAt = nowUtc;

            if (!customers.TryGetValue(booking.CustomerId, out var customer)
                || string.IsNullOrWhiteSpace(customer.Email))
            {
                skippedNoEmail++;
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Skipped BookingReminder event for booking {BookingNumber}: customer has no email.",
                        booking.BookingNumber);
                }

                continue;
            }

            string email = customer.Email;
            var invoiceNumber = booking.InvoiceId is Guid invoiceId ? invoiceNumbers.GetValueOrDefault(invoiceId) : null;

            var links = BookingCalendarLinkBuilder.Build(
                notificationSettings.PublicBaseUrl,
                booking.CalendarToken,
                new BookingIcsBuilder.BookingIcsData(
                    booking.Id,
                    booking.BookingNumber,
                    booking.PackageName,
                    rangeName,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.EndTime,
                    booking.ShooterCount),
                new BookingIcsBuilder.CompanyIcsData(
                    company.Name,
                    company.AddressLine1,
                    company.AddressLine2,
                    company.City,
                    company.Province,
                    company.PostalCode));

            lifecycleOutbox.Add(
                OutboxMessageTypes.BookingReminder,
                company,
                booking,
                rangeName,
                email,
                customer.Name,
                icsUrl: links.IcsUrl,
                googleCalendarUrl: links.GoogleCalendarUrl,
                depositAmount: null,
                depositDueAt: null,
                invoiceNumber: invoiceNumber);

            queued++;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new BookingReminderRunResult(queued, skippedNoEmail, skippedMissingRange);
    }

    private static bool IsRangeMissing(Dictionary<Guid, string> rangeNames, Guid shootingRangeId)
        => !rangeNames.ContainsKey(shootingRangeId);

    private sealed record BookingReminderRunResult(int Queued, int SkippedNoEmail, int SkippedMissingRange);
}
