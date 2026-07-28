using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.GetBookingAttendees;

public sealed class GetBookingAttendeesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetBookingAttendeesQuery, ErrorOr<IReadOnlyList<AttendeeResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<AttendeeResponse>>> Handle(
        GetBookingAttendeesQuery query, CancellationToken cancellationToken)
    {
        var attendees = await db.BookingAttendees
            .AsNoTracking()
            .Where(a => a.BookingId == query.BookingId)
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.Id)
            .Select(AttendeeResponse.QueryProjection)
            .ToListAsync(cancellationToken);

        return attendees;
    }
}
