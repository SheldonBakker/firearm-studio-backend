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

        if (request.FullName.IsSet)
        {
            attendee.FullName = request.FullName.Value;
        }

        if (request.IdNumber.IsSet)
        {
            attendee.IdNumber = request.IdNumber.Value;
        }

        if (request.LicenceNumber.IsSet)
        {
            attendee.LicenceNumber = request.LicenceNumber.Value;
        }

        if (request.FirearmMakeModel.IsSet)
        {
            attendee.FirearmMakeModel = request.FirearmMakeModel.Value;
        }

        if (request.FirearmSerialNumber.IsSet)
        {
            attendee.FirearmSerialNumber = request.FirearmSerialNumber.Value;
        }

        if (request.Calibre.IsSet)
        {
            attendee.Calibre = request.Calibre.Value;
        }

        if (request.FirearmOrigin.IsSet)
        {
            attendee.FirearmOrigin = request.FirearmOrigin.Value;
        }

        if (request.SignedIndemnity.IsSet)
        {
            attendee.SignedIndemnity = request.SignedIndemnity.Value;
        }

        if (request.Notes.IsSet)
        {
            attendee.Notes = request.Notes.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateAttendeeCommand.NotFound";
    }
}
