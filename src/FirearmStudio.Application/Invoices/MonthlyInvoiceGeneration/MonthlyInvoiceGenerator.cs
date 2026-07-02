using System.Globalization;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Invoices.MonthlyInvoiceGeneration;

public sealed class MonthlyInvoiceGenerator(
    IApplicationDbContext db,
    ITenantContext tenant,
    ILogger<MonthlyInvoiceGenerator> logger) : IMonthlyInvoiceGenerator
{
    public const decimal StandardVatRatePercent = 15m;

    public const int MinDueDays = 0;
    public const int MaxDueDays = 365;

    public async Task<MonthlyInvoiceGenerationResult> GenerateOutstandingAsync(
        string? vatNumber,
        int dueDays,
        CancellationToken cancellationToken)
    {
        if (tenant.CompanyId is null)
        {
            throw new InvalidOperationException(
                "Monthly invoice generation requires an active company context (ITenantContext.BeginCompanyScope); " +
                "without one the tenant query filter would silently match no rows.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
        var dueOn = today.AddDays(Math.Clamp(dueDays, MinDueDays, MaxDueDays));
        var chargeVat = !string.IsNullOrWhiteSpace(vatNumber);

        var storageRecords = await db.StorageRecords
            .AsNoTracking()
            .Where(record => record.StorageStatus != StorageStatus.Cancelled)
            .Include(record => record.Firearm)
            .ToListAsync(cancellationToken);

        var storage = storageRecords
            .Where(record => record.Firearm is not null)
            .ToList();

        if (storage.Count == 0)
        {
            return new MonthlyInvoiceGenerationResult(0, 0, 0);
        }

        var earliestFrom = storage.Min(record => record.StoredFrom);
        var firstMonth = new DateOnly(earliestFrom.Year, earliestFrom.Month, 1);

        var existingInvoices = await db.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.InvoiceMonth >= firstMonth)
            .Select(invoice => new { invoice.CustomerId, invoice.InvoiceMonth })
            .ToListAsync(cancellationToken);

        var invoicesByMonth = existingInvoices.ToLookup(invoice => invoice.InvoiceMonth);

        var created = 0;
        var skipped = 0;
        var monthsFailed = 0;

        for (var monthStart = firstMonth; monthStart <= currentMonthStart; monthStart = monthStart.AddMonths(1))
        {
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var monthInvoices = invoicesByMonth[monthStart].ToList();
            var billedCustomers = monthInvoices
                .Select(invoice => invoice.CustomerId)
                .ToHashSet();

            var storageByCustomer = storage
                .Where(record => record.StoredFrom <= monthEnd
                                 && (record.StoredUntil == null || record.StoredUntil >= monthStart))
                .GroupBy(record => record.Firearm!.CustomerId)
                .ToList();

            var monthLabel = monthStart.ToString("yyyyMM", CultureInfo.InvariantCulture);
            var humanMonth = monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
            var sequence = monthInvoices.Count;
            var monthCreated = 0;

            foreach (var group in storageByCustomer)
            {
                if (billedCustomers.Contains(group.Key))
                {
                    skipped++;
                    continue;
                }

                var subtotal = group.Sum(record => record.MonthlyRate);
                var vat = chargeVat
                    ? Math.Round(subtotal * StandardVatRatePercent / 100m, 2, MidpointRounding.AwayFromZero)
                    : 0m;

                sequence++;
                var invoiceId = Guid.CreateVersion7();
                db.Invoices.Add(new Invoice
                {
                    Id = invoiceId,
                    CustomerId = group.Key,
                    InvoiceNumber = $"INV-{monthLabel}-{sequence:D4}",
                    InvoiceMonth = monthStart,
                    Subtotal = subtotal,
                    VatAmount = vat,
                    Total = subtotal + vat,
                    Status = InvoiceStatus.Draft,
                    DueOn = dueOn,
                });

                foreach (var record in group)
                {
                    var firearm = record.Firearm!;
                    db.InvoiceLines.Add(new InvoiceLine
                    {
                        InvoiceId = invoiceId,
                        FirearmId = firearm.Id,
                        Description = $"Storage fee - {firearm.Make} {firearm.Model} - Serial: {firearm.SerialNumber} - {humanMonth}",
                        Quantity = 1,
                        UnitPrice = record.MonthlyRate,
                        LineTotal = record.MonthlyRate,
                    });
                }

                monthCreated++;
            }

            if (monthCreated == 0)
            {
                continue;
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                created += monthCreated;
            }
            catch (DbUpdateException ex)
            {
                db.ClearChangeTracker();
                monthsFailed++;
                logger.LogError(ex,
                    "Failed to save invoices for month {InvoiceMonth}; continuing with remaining months.",
                    monthStart);
            }
        }

        return new MonthlyInvoiceGenerationResult(created, skipped, monthsFailed);
    }
}
