using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Common;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.CheckInBooking;

public sealed class CheckInBookingCommandHandler(IApplicationDbContext db)
    : ICommandHandler<CheckInBookingCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(CheckInBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);
        if (booking is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Booking not found.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            return Error.Conflict(ErrorCodes.NotConfirmed, "Only confirmed bookings can be checked in.");
        }

        var today = BusinessDate.Today();
        if (booking.BookingDate != today)
        {
            return Error.Conflict(ErrorCodes.WrongDay, "A booking can only be checked in on its booking date.");
        }

        if (booking.CheckedInAt is not null)
        {
            return Error.Conflict(ErrorCodes.AlreadyCheckedIn, "The booking has already been checked in.");
        }

        booking.CheckedInAt = DateTime.UtcNow;

        foreach (var attendeeRequest in command.Request.Attendees)
        {
            await db.BookingAttendees.AddAsync(new BookingAttendee
            {
                BookingId = booking.Id,
                FullName = attendeeRequest.FullName,
                IdNumber = attendeeRequest.IdNumber,
                LicenceNumber = attendeeRequest.LicenceNumber,
                FirearmMakeModel = attendeeRequest.FirearmMakeModel,
                FirearmSerialNumber = attendeeRequest.FirearmSerialNumber,
                Calibre = attendeeRequest.Calibre,
                FirearmOrigin = attendeeRequest.FirearmOrigin,
                SignedIndemnity = attendeeRequest.SignedIndemnity,
                Notes = attendeeRequest.Notes,
            }, cancellationToken);
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
        public const string NotFound = "CheckInBookingCommand.NotFound";
        public const string NotConfirmed = "CheckInBookingCommand.NotConfirmed";
        public const string WrongDay = "CheckInBookingCommand.WrongDay";
        public const string AlreadyCheckedIn = "CheckInBookingCommand.AlreadyCheckedIn";
        public const string ConcurrentUpdate = "CheckInBookingCommand.ConcurrentUpdate";
    }
}
