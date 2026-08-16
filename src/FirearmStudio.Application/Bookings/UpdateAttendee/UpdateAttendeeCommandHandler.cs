using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.UpdateAttendee;

public sealed class UpdateAttendeeCommandHandler(IApplicationDbContext db)
    : ICommandHandler<UpdateAttendeeCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(UpdateAttendeeCommand command, CancellationToken cancellationToken)
    {
        var attendee = await db.BookingAttendees.FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);
        if (attendee is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Attendee not found.");
        }

        var request = command.Request;
        request.FullName.ApplyTo(v => attendee.FullName = v);
        request.IdNumber.ApplyTo(v => attendee.IdNumber = v);
        request.LicenceNumber.ApplyTo(v => attendee.LicenceNumber = v);
        request.FirearmMakeModel.ApplyTo(v => attendee.FirearmMakeModel = v);
        request.FirearmSerialNumber.ApplyTo(v => attendee.FirearmSerialNumber = v);
        request.Calibre.ApplyTo(v => attendee.Calibre = v);
        request.FirearmOrigin.ApplyTo(v => attendee.FirearmOrigin = v);
        request.SignedIndemnity.ApplyTo(v => attendee.SignedIndemnity = v);
        request.Notes.ApplyTo(v => attendee.Notes = v);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateAttendeeCommand.NotFound";
    }
}
