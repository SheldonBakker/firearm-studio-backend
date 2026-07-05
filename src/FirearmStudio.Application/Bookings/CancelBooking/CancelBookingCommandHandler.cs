using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.CancelBooking;

public sealed class CancelBookingCommandHandler(IApplicationDbContext db)
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
