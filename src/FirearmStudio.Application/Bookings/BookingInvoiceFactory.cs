using System.Globalization;
using FirearmStudio.Application.Invoices.MonthlyInvoiceGeneration;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Bookings;

internal static class BookingInvoiceFactory
{
    internal sealed record IncludedItem(string Description, decimal Quantity);

    internal static Invoice Create(
        Booking booking,
        string? companyVatNumber,
        int companyDueDays,
        string rangeName,
        IReadOnlyList<IncludedItem> packageItems)
    {
        var invoiceId = Guid.CreateVersion7();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var subtotal = booking.PackagePrice;
        var chargeVat = !string.IsNullOrWhiteSpace(companyVatNumber);
        var vat = chargeVat
            ? Math.Round(subtotal * MonthlyInvoiceGenerator.StandardVatRatePercent / 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var dateLabel = booking.BookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var invoice = new Invoice
        {
            Id = invoiceId,
            CustomerId = booking.CustomerId,
            InvoiceNumber = $"INV-{booking.BookingNumber}",
            InvoiceMonth = new DateOnly(booking.BookingDate.Year, booking.BookingDate.Month, 1),
            Kind = InvoiceKind.Booking,
            Subtotal = subtotal,
            VatAmount = vat,
            Total = subtotal + vat,
            Status = InvoiceStatus.Draft,
            DueOn = today.AddDays(Math.Clamp(companyDueDays, MonthlyInvoiceGenerator.MinDueDays, MonthlyInvoiceGenerator.MaxDueDays)),
        };

        invoice.Lines.Add(new InvoiceLine
        {
            InvoiceId = invoiceId,
            Description = $"Range booking {rangeName} - {booking.PackageName} - {dateLabel} {booking.StartTime:HH\\:mm}-{booking.EndTime:HH\\:mm} - {booking.BookingNumber}",
            Quantity = 1,
            UnitPrice = subtotal,
            LineTotal = subtotal,
        });

        foreach (var item in packageItems)
        {
            invoice.Lines.Add(new InvoiceLine
            {
                InvoiceId = invoiceId,
                Description = $"Included: {item.Description}",
                Quantity = item.Quantity,
                UnitPrice = 0,
                LineTotal = 0,
            });
        }

        return invoice;
    }

    internal sealed record BookingLine(
        Booking Booking,
        string RangeName,
        IReadOnlyList<IncludedItem> PackageItems);

    /// <summary>
    /// Builds a single draft invoice covering several bookings (a multi-session cart checkout).
    /// One priced line per booking, plus zero-priced lines for each booking's included package items.
    /// </summary>
    internal static Invoice CreateCombined(
        IReadOnlyList<BookingLine> bookings,
        string? companyVatNumber,
        int companyDueDays)
    {
        var invoiceId = Guid.CreateVersion7();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var first = bookings[0].Booking;
        var subtotal = bookings.Sum(b => b.Booking.PackagePrice);
        var chargeVat = !string.IsNullOrWhiteSpace(companyVatNumber);
        var vat = chargeVat
            ? Math.Round(subtotal * MonthlyInvoiceGenerator.StandardVatRatePercent / 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var earliest = bookings.Min(b => b.Booking.BookingDate);

        var invoice = new Invoice
        {
            Id = invoiceId,
            CustomerId = first.CustomerId,
            InvoiceNumber = $"INV-{first.BookingNumber}",
            InvoiceMonth = new DateOnly(earliest.Year, earliest.Month, 1),
            Kind = InvoiceKind.Booking,
            Subtotal = subtotal,
            VatAmount = vat,
            Total = subtotal + vat,
            Status = InvoiceStatus.Draft,
            DueOn = today.AddDays(Math.Clamp(companyDueDays, MonthlyInvoiceGenerator.MinDueDays, MonthlyInvoiceGenerator.MaxDueDays)),
        };

        foreach (var line in bookings)
        {
            var booking = line.Booking;
            var dateLabel = booking.BookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            invoice.Lines.Add(new InvoiceLine
            {
                InvoiceId = invoiceId,
                Description = $"Range booking {line.RangeName} - {booking.PackageName} - {dateLabel} {booking.StartTime:HH\\:mm}-{booking.EndTime:HH\\:mm} - {booking.BookingNumber}",
                Quantity = 1,
                UnitPrice = booking.PackagePrice,
                LineTotal = booking.PackagePrice,
            });

            foreach (var item in line.PackageItems)
            {
                invoice.Lines.Add(new InvoiceLine
                {
                    InvoiceId = invoiceId,
                    Description = $"Included: {item.Description}",
                    Quantity = item.Quantity,
                    UnitPrice = 0,
                    LineTotal = 0,
                });
            }
        }

        return invoice;
    }
}
