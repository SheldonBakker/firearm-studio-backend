using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Bookings;
using FirearmStudio.Application.Common;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.BackgroundJobs;

public sealed class BookingDepositExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingDepositExpiryService> logger) : BackgroundService
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
                logger.LogError(ex, "Booking deposit expiry run failed.");
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
                        "Skipping booking deposit expiry: {Count} pending database migration(s): {Migrations}. " +
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

        foreach (var companyId in companyIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var scope = scopeFactory.CreateScope();
                var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var lifecycleOutbox = scope.ServiceProvider.GetRequiredService<IBookingLifecycleOutbox>();

                using (tenant.BeginCompanyScope(companyId))
                {
                    var result = await RunForCompanyAsync(db, lifecycleOutbox, companyId, nowUtc, cancellationToken);

                    if ((result.Cancelled > 0 || result.SkippedNoEmail > 0 || result.InvoicesLeftOpen > 0)
                        && logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation(
                            "Booking deposit expiry for company {CompanyId}: {Cancelled} cancelled, {SkippedNoEmail} " +
                            "skipped (no email), {InvoicesLeftOpen} invoice(s) left open (has payments).",
                            companyId, result.Cancelled, result.SkippedNoEmail, result.InvoicesLeftOpen);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Booking deposit expiry failed for company {CompanyId}.", companyId);
            }
        }
    }

    private async Task<BookingDepositExpiryRunResult> RunForCompanyAsync(
        IApplicationDbContext db,
        IBookingLifecycleOutbox lifecycleOutbox,
        Guid companyId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var expired = await db.Bookings
            .Where(b => b.Status == BookingStatus.Pending && b.Source == BookingSource.Public)
            .Join(
                db.Invoices,
                booking => booking.InvoiceId,
                invoice => invoice.Id,
                (booking, invoice) => new
                {
                    Booking = booking,
                    Invoice = invoice,
                    HasPayments = invoice.Payments.Any(),
                })
            .Where(x => x.Invoice.DepositAmount != null
                && x.Invoice.DepositPaidAt == null
                && x.Invoice.DepositDueAt < nowUtc)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return new BookingDepositExpiryRunResult(0, 0, 0);
        }

        var company = await db.Companies
            .AsNoTracking()
            .FirstAsync(c => c.Id == companyId, cancellationToken);

        var rangeIds = expired.Select(x => x.Booking.ShootingRangeId).Distinct().ToList();
        var rangeNames = await db.ShootingRanges
            .AsNoTracking()
            .Where(r => rangeIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var customerIds = expired.Select(x => x.Booking.CustomerId).Distinct().ToList();
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

        var cancelled = 0;
        var skippedNoEmail = 0;
        var invoicesLeftOpen = 0;

        foreach (var row in expired)
        {
            var booking = row.Booking;
            var invoice = row.Invoice;

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = nowUtc;
            cancelled++;

            if (row.HasPayments)
            {
                invoicesLeftOpen++;
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(
                        "Deposit expired for booking {BookingNumber} but invoice {InvoiceNumber} has recorded " +
                        "payments; leaving the invoice open for manual review.",
                        booking.BookingNumber, invoice.InvoiceNumber);
                }
            }
            else if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Sent or InvoiceStatus.Overdue)
            {
                invoice.Status = InvoiceStatus.Cancelled;
            }

            // A dangling ShootingRangeId FK is a data-quality problem, not a reason to abandon
            // this booking's cancellation: log it and carry on with a null range name rather than
            // letting one bad booking sink the rest of this tenant's batch.
            if (!rangeNames.TryGetValue(booking.ShootingRangeId, out var rangeName))
            {
                rangeName = null;
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(
                        "BookingCancelled event for booking {BookingNumber} has no range name: shooting range " +
                        "{ShootingRangeId} not found.",
                        booking.BookingNumber, booking.ShootingRangeId);
                }
            }

            if (!customers.TryGetValue(booking.CustomerId, out var customer)
                || string.IsNullOrWhiteSpace(customer.Email))
            {
                skippedNoEmail++;
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Skipped BookingCancelled event for booking {BookingNumber}: customer has no email.",
                        booking.BookingNumber);
                }

                continue;
            }

            lifecycleOutbox.Add(
                OutboxMessageTypes.BookingCancelled,
                company,
                booking,
                rangeName,
                customer.Email,
                customer.Name,
                icsUrl: null,
                googleCalendarUrl: null,
                depositAmount: null,
                depositDueAt: null,
                invoiceNumber: invoice.InvoiceNumber);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new BookingDepositExpiryRunResult(cancelled, skippedNoEmail, invoicesLeftOpen);
    }

    private sealed record BookingDepositExpiryRunResult(int Cancelled, int SkippedNoEmail, int InvoicesLeftOpen);
}
