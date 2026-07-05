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
        var monthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
        var dueOn = today.AddDays(Math.Clamp(dueDays, MinDueDays, MaxDueDays));
        var chargeVat = !string.IsNullOrWhiteSpace(vatNumber);

        var storageRecords = await db.StorageRecords
            .AsNoTracking()
            .Where(record => record.StorageStatus == StorageStatus.Active
                             && record.StoredFrom <= monthEnd)
            .Include(record => record.Firearm)
            .ToListAsync(cancellationToken);

        var storageByCustomer = storageRecords
            .Where(record => record.Firearm is not null)
            .GroupBy(record => record.Firearm!.CustomerId)
            .ToList();

        if (storageByCustomer.Count == 0)
        {
            return new MonthlyInvoiceGenerationResult(0, 0, 0);
        }

        var billedCustomers = await db.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.InvoiceMonth == currentMonthStart
                              && invoice.Kind == InvoiceKind.MonthlyStorage)
            .Select(invoice => invoice.CustomerId)
            .ToListAsync(cancellationToken);

        var billedCustomerSet = billedCustomers.ToHashSet();

        var monthLabel = currentMonthStart.ToString("yyyyMM", CultureInfo.InvariantCulture);
        var humanMonth = currentMonthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        var sequence = billedCustomers.Count;

        var created = 0;
        var skipped = 0;

        foreach (var group in storageByCustomer)
        {
            if (billedCustomerSet.Contains(group.Key))
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
                InvoiceMonth = currentMonthStart,
                Kind = InvoiceKind.MonthlyStorage,
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

            created++;
        }

        if (created == 0)
        {
            return new MonthlyInvoiceGenerationResult(0, skipped, 0);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            db.ClearChangeTracker();
            logger.LogError(ex,
                "Failed to save invoices for month {InvoiceMonth}.",
                currentMonthStart);
            return new MonthlyInvoiceGenerationResult(0, skipped, 1);
        }

        return new MonthlyInvoiceGenerationResult(created, skipped, 0);
    }
}
