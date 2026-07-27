using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.RemoveAttendee;

public sealed class RemoveAttendeeCommandHandler(IApplicationDbContext db)
    : ICommandHandler<RemoveAttendeeCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(RemoveAttendeeCommand command, CancellationToken cancellationToken)
    {
        var attendee = await db.BookingAttendees.FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);
        if (attendee is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Attendee not found.");
        }

        db.BookingAttendees.Remove(attendee);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "RemoveAttendeeCommand.NotFound";
    }
}
