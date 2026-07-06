using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.ConfirmBooking;

public sealed class ConfirmBookingCommandHandler(IApplicationDbContext db)
    : ICommandHandler<ConfirmBookingCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(ConfirmBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings
            .Include(b => b.ShootingRange)
            .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

        if (booking is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Booking not found.");
        }

        if (booking.Status != BookingStatus.Pending)
        {
            return Error.Conflict(ErrorCodes.NotPending, "Only pending bookings can be confirmed.");
        }

        // Bookings created through the public cart already belong to a single combined invoice.
        // Only generate a per-booking invoice when the booking is not already invoiced, so
        // confirming a multi-session cart does not spawn one extra invoice per booking.
        if (booking.InvoiceId is null)
        {
            var company = await db.Companies
                .AsNoTracking()
                .Where(c => c.Id == booking.CompanyId)
                .Select(c => new { c.VatNumber, c.DueDays })
                .FirstAsync(cancellationToken);

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
                booking.ShootingRange!.Name,
                packageItems);

            db.Invoices.Add(invoice);
            booking.InvoiceId = invoice.Id;
        }

        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedAt = DateTime.UtcNow;

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
