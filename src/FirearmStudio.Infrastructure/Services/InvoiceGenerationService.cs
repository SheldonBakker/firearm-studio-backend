using System.Globalization;
using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Invoices;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Infrastructure.Services;

public sealed class InvoiceGenerationService(IApplicationDbContext db, ITenantContext tenant) : IInvoiceGenerationService
{
    public async Task<ErrorOr<GenerateMonthlyInvoicesResponse>> GenerateMonthlyAsync(
        GenerateMonthlyInvoicesRequest request, CancellationToken ct = default)
    {
        if (tenant.CompanyId is null)
        {
            return Error.Forbidden(description: "No company context is available for invoice generation.");
        }

        var monthStart = new DateOnly(request.InvoiceMonth.Year, request.InvoiceMonth.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var activeStorage = await db.StorageRecords
            .Where(s => s.StorageStatus == StorageStatus.Active
                        && s.StoredFrom <= monthEnd
                        && (s.StoredUntil == null || s.StoredUntil >= monthStart))
            .Include(s => s.Firearm)
            .ToListAsync(ct);

        var existing = await db.Invoices
            .Where(i => i.InvoiceMonth == monthStart)
            .Select(i => new { i.Id, i.CustomerId, i.Status })
            .ToListAsync(ct);

        var billedCustomers = existing
            .Where(e => e.Status != InvoiceStatus.Cancelled)
            .Select(e => e.CustomerId)
            .ToHashSet();
        var cancelledInvoiceByCustomer = existing
            .Where(e => e.Status == InvoiceStatus.Cancelled)
            .GroupBy(e => e.CustomerId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var byCustomer = activeStorage
            .Where(s => s.Firearm is not null)
            .GroupBy(s => s.Firearm!.CustomerId)
            .ToList();

        var monthLabel = monthStart.ToString("yyyyMM", CultureInfo.InvariantCulture);
        var humanMonth = monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        var dueOn = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(request.DueDays);
        var sequence = existing.Count;

        var created = 0;
        var skipped = 0;

        async Task AddLinesAsync(Guid invoiceId, IEnumerable<StorageRecord> records)
        {
            foreach (var storage in records)
            {
                var f = storage.Firearm!;
                await db.InvoiceLines.AddAsync(new InvoiceLine
                {
                    InvoiceId = invoiceId,
                    FirearmId = f.Id,
                    Description = $"Storage fee - {f.Make} {f.Model} - Serial: {f.SerialNumber} - {humanMonth}",
                    Quantity = 1,
                    UnitPrice = storage.MonthlyRate,
                    LineTotal = storage.MonthlyRate,
                }, ct);
            }
        }

        foreach (var group in byCustomer)
        {
            if (billedCustomers.Contains(group.Key))
            {
                skipped++;
                continue;
            }

            var subtotal = group.Sum(s => s.MonthlyRate);
            var vat = Math.Round(subtotal * request.VatRate / 100m, 2, MidpointRounding.AwayFromZero);

            if (cancelledInvoiceByCustomer.TryGetValue(group.Key, out var cancelledId))
            {
                var invoice = await db.Invoices.Include(i => i.Lines).FirstAsync(i => i.Id == cancelledId, ct);
                db.InvoiceLines.RemoveRange(invoice.Lines);
                invoice.Subtotal = subtotal;
                invoice.VatAmount = vat;
                invoice.Total = subtotal + vat;
                invoice.Status = InvoiceStatus.Draft;
                invoice.SentAt = null;
                invoice.DueOn = dueOn;
                await AddLinesAsync(invoice.Id, group);
            }
            else
            {
                sequence++;
                var invoiceId = Guid.CreateVersion7();
                await db.Invoices.AddAsync(new Invoice
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
                }, ct);
                await AddLinesAsync(invoiceId, group);
            }

            created++;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Error.Conflict(
                description: "Invoice generation conflicted with another run. Please retry.");
        }

        return new GenerateMonthlyInvoicesResponse(created, skipped);
    }
}
