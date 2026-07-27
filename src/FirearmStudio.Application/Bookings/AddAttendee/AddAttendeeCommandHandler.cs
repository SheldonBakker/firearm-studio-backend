using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.AddAttendee;

public sealed class AddAttendeeCommandHandler(IApplicationDbContext db)
    : ICommandHandler<AddAttendeeCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(AddAttendeeCommand command, CancellationToken cancellationToken)
    {
        var bookingExists = await db.Bookings.AnyAsync(b => b.Id == command.BookingId, cancellationToken);
        if (!bookingExists)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Booking not found.");
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
    }
}
