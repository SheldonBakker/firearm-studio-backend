using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Bookings.ConfirmBooking;

public sealed class ConfirmBookingCommandHandler(
    IApplicationDbContext db,
    IBookingLifecycleOutbox lifecycleOutbox,
    NotificationSettings notificationSettings,
    ILogger<ConfirmBookingCommandHandler> logger)
    : ICommandHandler<ConfirmBookingCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(ConfirmBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

        if (booking is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Booking not found.");
        }

        if (booking.Status != BookingStatus.Pending)
        {
            return Error.Conflict(ErrorCodes.NotPending, "Only pending bookings can be confirmed.");
        }

        var company = await db.Companies
            .AsNoTracking()
            .Where(c => c.Id == booking.CompanyId)
            .FirstAsync(cancellationToken);

        var rangeName = await db.ShootingRanges
            .AsNoTracking()
            .Where(r => r.Id == booking.ShootingRangeId)
            .Select(r => r.Name)
            .FirstAsync(cancellationToken);

        var customer = await db.Customers
            .AsNoTracking()
            .Where(c => c.Id == booking.CustomerId)
            .Select(c => new
            {
                c.Email,
                Name = c.CustomerType == CustomerType.Company ? c.CompanyName : c.FullName,
            })
            .FirstAsync(cancellationToken);

        string? invoiceNumber;

        // Bookings created through the public cart already belong to a single combined invoice.
        // Only generate a per-booking invoice when the booking is not already invoiced, so
        // confirming a multi-session cart does not spawn one extra invoice per booking.
        if (booking.InvoiceId is null)
        {
            var packageItems = await db.PackageItems
                .AsNoTracking()
                .Where(i => i.PackageId == booking.PackageId)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .Select(i => new BookingInvoiceFactory.IncludedItem(i.Description, i.Quantity))
                .ToListAsync(cancellationToken);

            var invoice = BookingInvoiceFactory.Create(
                booking,
                company.VatNumber,
                company.DueDays,
                rangeName,
                packageItems);

            db.Invoices.Add(invoice);
            booking.InvoiceId = invoice.Id;
            invoiceNumber = invoice.InvoiceNumber;
        }
        else
        {
            invoiceNumber = await db.Invoices
                .AsNoTracking()
                .Where(i => i.Id == booking.InvoiceId)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Skipped BookingConfirmed event for booking {BookingNumber}: customer has no email.",
                    booking.BookingNumber);
            }
        }
        else
        {
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
                OutboxMessageTypes.BookingConfirmed,
                company,
                booking,
                rangeName,
                customer.Email,
                customer.Name,
                icsUrl: links.IcsUrl,
                googleCalendarUrl: links.GoogleCalendarUrl,
                depositAmount: null,
                depositDueAt: null,
                invoiceNumber: invoiceNumber);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error.Conflict(ErrorCodes.ConcurrentUpdate, "The booking was modified concurrently.");
        }

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "ConfirmBookingCommand.NotFound";
        public const string NotPending = "ConfirmBookingCommand.NotPending";
        public const string ConcurrentUpdate = "ConfirmBookingCommand.ConcurrentUpdate";
    }
}
