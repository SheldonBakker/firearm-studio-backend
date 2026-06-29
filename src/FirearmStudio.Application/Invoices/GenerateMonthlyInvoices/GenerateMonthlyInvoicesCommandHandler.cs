using System.Globalization;
using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Invoices.GenerateMonthlyInvoices;

public sealed class GenerateMonthlyInvoicesCommandHandler(
    IApplicationDbContext db,
    ITenantContext tenant)
    : ICommandHandler<GenerateMonthlyInvoicesCommand, ErrorOr<GenerateMonthlyInvoicesResponse>>
{
    public async Task<ErrorOr<GenerateMonthlyInvoicesResponse>> Handle(
        GenerateMonthlyInvoicesCommand command,
        CancellationToken cancellationToken)
    {
        if (tenant.CompanyId is null)
        {
            return Error.Forbidden(ErrorCodes.CompanyContextMissing, "No company context is available for invoice generation.");
        }

        var request = command.Request;
        var monthStart = new DateOnly(request.InvoiceMonth.Year, request.InvoiceMonth.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var activeStorage = await db.StorageRecords
            .AsNoTracking()
            .Where(record => record.StorageStatus == StorageStatus.Active
                             && record.StoredFrom <= monthEnd
                             && (record.StoredUntil == null || record.StoredUntil >= monthStart))
            .Include(record => record.Firearm)
            .ToListAsync(cancellationToken);

        var existingInvoices = await db.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.InvoiceMonth == monthStart)
            .Select(invoice => new { invoice.Id, invoice.CustomerId, invoice.Status })
            .ToListAsync(cancellationToken);

        var billedCustomers = existingInvoices
            .Where(invoice => invoice.Status != InvoiceStatus.Cancelled)
            .Select(invoice => invoice.CustomerId)
            .ToHashSet();
        var cancelledInvoiceByCustomer = existingInvoices
            .Where(invoice => invoice.Status == InvoiceStatus.Cancelled)
            .GroupBy(invoice => invoice.CustomerId)
            .ToDictionary(group => group.Key, group => group.First().Id);

        var storageByCustomer = activeStorage
            .Where(record => record.Firearm is not null)
            .GroupBy(record => record.Firearm!.CustomerId)
            .ToList();

        var monthLabel = monthStart.ToString("yyyyMM", CultureInfo.InvariantCulture);
        var humanMonth = monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        var dueOn = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(request.DueDays);
        var sequence = existingInvoices.Count;
        var created = 0;
        var skipped = 0;

        async Task AddLinesAsync(Guid invoiceId, IEnumerable<StorageRecord> records)
        {
            foreach (var storage in records)
            {
                var firearm = storage.Firearm!;
                await db.InvoiceLines.AddAsync(new InvoiceLine
                {
                    InvoiceId = invoiceId,
                    FirearmId = firearm.Id,
                    Description = $"Storage fee - {firearm.Make} {firearm.Model} - Serial: {firearm.SerialNumber} - {humanMonth}",
                    Quantity = 1,
                    UnitPrice = storage.MonthlyRate,
                    LineTotal = storage.MonthlyRate,
                }, cancellationToken);
            }
        }

        foreach (var group in storageByCustomer)
        {
            if (billedCustomers.Contains(group.Key))
            {
                skipped++;
                continue;
            }

            var subtotal = group.Sum(record => record.MonthlyRate);
            var vat = Math.Round(subtotal * request.VatRate / 100m, 2, MidpointRounding.AwayFromZero);

            if (cancelledInvoiceByCustomer.TryGetValue(group.Key, out var cancelledInvoiceId))
            {
                var invoice = await db.Invoices
                    .Include(candidate => candidate.Lines)
                    .FirstAsync(candidate => candidate.Id == cancelledInvoiceId, cancellationToken);
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
                }, cancellationToken);
                await AddLinesAsync(invoiceId, group);
            }

            created++;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Error.Conflict(ErrorCodes.ConcurrentGeneration, "Invoice generation conflicted with another run. Please retry.");
        }

        return new GenerateMonthlyInvoicesResponse(created, skipped);
    }

    public static class ErrorCodes
    {
        public const string CompanyContextMissing = "GenerateMonthlyInvoicesCommand.CompanyContextMissing";
        public const string ConcurrentGeneration = "GenerateMonthlyInvoicesCommand.ConcurrentGeneration";
    }
}
