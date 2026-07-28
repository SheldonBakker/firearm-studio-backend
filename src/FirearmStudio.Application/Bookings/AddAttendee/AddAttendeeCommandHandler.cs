using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.AddAttendee;

public sealed class AddAttendeeCommandHandler(IApplicationDbContext db)
    : ICommandHandler<AddAttendeeCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(AddAttendeeCommand command, CancellationToken cancellationToken)
    {
        var status = await db.Bookings
            .Where(b => b.Id == command.BookingId)
            .Select(b => (BookingStatus?)b.Status)
            .FirstOrDefaultAsync(cancellationToken);

        if (status is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Booking not found.");
        }

        if (status is BookingStatus.Cancelled or BookingStatus.NoShow)
        {
            return Error.Conflict(ErrorCodes.BookingNotActive, "Attendees cannot be added to a cancelled or no-show booking.");
        }

        var request = command.Request;
        var attendee = new BookingAttendee
        {
            BookingId = command.BookingId,
            FullName = request.FullName,
            IdNumber = request.IdNumber,
            LicenceNumber = request.LicenceNumber,
            FirearmMakeModel = request.FirearmMakeModel,
            FirearmSerialNumber = request.FirearmSerialNumber,
            Calibre = request.Calibre,
            FirearmOrigin = request.FirearmOrigin,
            SignedIndemnity = request.SignedIndemnity,
            Notes = request.Notes,
        };

        await db.BookingAttendees.AddAsync(attendee, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return attendee.Id;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "AddAttendeeCommand.NotFound";
        public const string BookingNotActive = "AddAttendeeCommand.BookingNotActive";
    }
}
