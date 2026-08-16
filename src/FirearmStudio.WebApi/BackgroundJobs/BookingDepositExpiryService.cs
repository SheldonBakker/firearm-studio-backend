using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Bookings;
using FirearmStudio.Application.Common;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.BackgroundJobs;

public sealed class BookingDepositExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingDepositExpiryService> logger)
    : PeriodicJobBase(scopeFactory, logger)
{
    protected override TimeSpan Interval => TimeSpan.FromHours(1);
    protected override void LogRunFailed(Exception ex) =>
        logger.LogError(ex, "Booking deposit expiry run failed.");

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        List<Guid> companyIds;
        using (var scope = ScopeFactory.CreateScope())
        {
            if (!await EnsureMigrationsVerifiedAsync(scope, "booking deposit expiry", cancellationToken))
            {
                return;
            }

            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            companyIds = await db.Companies
                .AsNoTracking()
                .Where(company => company.IsActive)
                .Select(company => company.Id)
                .ToListAsync(cancellationToken);
        }

        var nowUtc = DateTime.UtcNow;

        await RunForAllCompaniesAsync(
            companyIds,
            static id => id,
            async (scope, companyId, ct) =>
            {
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var lifecycleOutbox = scope.ServiceProvider.GetRequiredService<IBookingLifecycleOutbox>();

                var result = await RunForCompanyAsync(db, lifecycleOutbox, companyId, nowUtc, ct);

                if ((result.Cancelled > 0 || result.SkippedNoEmail > 0 || result.InvoicesLeftOpen > 0)
                    && Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation(
                        "Booking deposit expiry for company {CompanyId}: {Cancelled} cancelled, {SkippedNoEmail} " +
                        "skipped (no email), {InvoicesLeftOpen} invoice(s) left open (has payments).",
                        companyId, result.Cancelled, result.SkippedNoEmail, result.InvoicesLeftOpen);
                }
            },
            (ex, id) => logger.LogError(ex, "Booking deposit expiry failed for company {CompanyId}.", id),
            cancellationToken);
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

            var rangeName = ResolveRangeNameOrNullForCancellation(
                booking.ShootingRangeId, rangeNames, booking.BookingNumber);

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

    private string? ResolveRangeNameOrNullForCancellation(
        Guid rangeId,
        Dictionary<Guid, string> rangeNames,
        string bookingNumber)
    {
        if (rangeNames.TryGetValue(rangeId, out var name))
        {
            return name;
        }

        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                "BookingCancelled event for booking {BookingNumber} has no range name: shooting range " +
                "{ShootingRangeId} not found.",
                bookingNumber, rangeId);
        }

        return null;
    }

    private sealed record BookingDepositExpiryRunResult(int Cancelled, int SkippedNoEmail, int InvoicesLeftOpen);
}
