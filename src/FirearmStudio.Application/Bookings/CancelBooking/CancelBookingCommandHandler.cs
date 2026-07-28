using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Bookings.CancelBooking;

public sealed class CancelBookingCommandHandler(
    IApplicationDbContext db,
    IBookingLifecycleOutbox lifecycleOutbox,
    ILogger<CancelBookingCommandHandler> logger)
    : ICommandHandler<CancelBookingCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(CancelBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);
        if (booking is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Booking not found.");
        }

        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
        {
            return Error.Conflict(ErrorCodes.AlreadyFinalised, "The booking is already finalised.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;

        string? invoiceNumber = null;

        if (booking.InvoiceId is Guid invoiceId)
        {
            var invoiceInfo = await db.Invoices
                .Where(i => i.Id == invoiceId)
                .Select(i => new
                {
                    Invoice = i,
                    HasPayments = i.Payments.Any(),
                    OtherActiveCount = db.Bookings.Count(b =>
                        b.InvoiceId == i.Id &&
                        b.Id != command.Id &&
                        b.Status != BookingStatus.Cancelled),
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (invoiceInfo is not null)
            {
                invoiceNumber = invoiceInfo.Invoice.InvoiceNumber;

                if (invoiceInfo.OtherActiveCount == 0
                    && !invoiceInfo.HasPayments
                    && invoiceInfo.Invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Sent or InvoiceStatus.Overdue)
                {
                    invoiceInfo.Invoice.Status = InvoiceStatus.Cancelled;
                }
            }
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

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Skipped BookingCancelled event for booking {BookingNumber}: customer has no email.",
                    booking.BookingNumber);
            }
        }
        else
        {
            // The event is added to a calendar; a cancelled booking has nothing to add.
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
        public const string NotFound = "CancelBookingCommand.NotFound";
        public const string AlreadyFinalised = "CancelBookingCommand.AlreadyFinalised";
        public const string ConcurrentUpdate = "CancelBookingCommand.ConcurrentUpdate";
    }
}
