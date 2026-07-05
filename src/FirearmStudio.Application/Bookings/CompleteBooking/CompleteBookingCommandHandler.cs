using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.CompleteBooking;

public sealed class CompleteBookingCommandHandler(IApplicationDbContext db)
    : ICommandHandler<CompleteBookingCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(CompleteBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);
        if (booking is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Booking not found.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            return Error.Conflict(ErrorCodes.NotConfirmed, "Only confirmed bookings can be completed.");
        }

        booking.Status = BookingStatus.Completed;

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
        public const string NotFound = "CompleteBookingCommand.NotFound";
        public const string NotConfirmed = "CompleteBookingCommand.NotConfirmed";
        public const string ConcurrentUpdate = "CompleteBookingCommand.ConcurrentUpdate";
    }
}
