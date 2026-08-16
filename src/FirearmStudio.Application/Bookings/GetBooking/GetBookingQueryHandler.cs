using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.GetBooking;

public sealed class GetBookingQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetBookingQuery, ErrorOr<BookingResponse>>
{
    public async Task<ErrorOr<BookingResponse>> Handle(GetBookingQuery query, CancellationToken cancellationToken)
    {
        return await db.Bookings
            .AsNoTracking()
            .Where(b => b.Id == query.Id)
            .FirstOrNotFoundAsync(BookingResponse.QueryProjection, ErrorCodes.NotFound, "Booking not found.", cancellationToken);
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetBookingQuery.NotFound";
    }
}
