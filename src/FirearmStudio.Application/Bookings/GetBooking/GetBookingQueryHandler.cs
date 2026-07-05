using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.GetBooking;

public sealed class GetBookingQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetBookingQuery, ErrorOr<BookingResponse>>
{
    public async Task<ErrorOr<BookingResponse>> Handle(GetBookingQuery query, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings
            .AsNoTracking()
            .Where(b => b.Id == query.Id)
            .Select(BookingResponse.QueryProjection)
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Booking not found.");
        }

        return booking;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetBookingQuery.NotFound";
    }
}
