using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.MarkBookingNoShow;

public sealed class MarkBookingNoShowCommandHandler(IApplicationDbContext db)
    : ICommandHandler<MarkBookingNoShowCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(MarkBookingNoShowCommand command, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);
        if (booking is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Booking not found.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            return Error.Conflict(ErrorCodes.NotConfirmed, "Only confirmed bookings can be marked as a no-show.");
        }

        booking.Status = BookingStatus.NoShow;

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
        public const string NotFound = "MarkBookingNoShowCommand.NotFound";
        public const string NotConfirmed = "MarkBookingNoShowCommand.NotConfirmed";
        public const string ConcurrentUpdate = "MarkBookingNoShowCommand.ConcurrentUpdate";
    }
}
