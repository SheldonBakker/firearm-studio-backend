using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.GetBookingCalendar;

public sealed class GetBookingCalendarQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetBookingCalendarQuery, ErrorOr<IReadOnlyList<BookingCalendarItemDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<BookingCalendarItemDto>>> Handle(
        GetBookingCalendarQuery query, CancellationToken cancellationToken)
    {
        if (query.Year is < 2000 or > 2100 || query.Month is < 1 or > 12)
        {
            return Error.Validation(ErrorCodes.InvalidMonth, "Year or month is out of range.");
        }

        var monthStart = new DateOnly(query.Year, query.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var queryable = db.Bookings
            .AsNoTracking()
            .Where(b => b.BookingDate >= monthStart
                        && b.BookingDate < nextMonthStart
                        && b.Status != BookingStatus.Cancelled);

        if (query.ShootingRangeId.HasValue)
        {
            queryable = queryable.Where(b => b.ShootingRangeId == query.ShootingRangeId.Value);
        }

        var items = await queryable
            .OrderBy(b => b.BookingDate)
            .ThenBy(b => b.StartTime)
            .ThenBy(b => b.Id)
            .Select(BookingCalendarItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return items;
    }

    public static class ErrorCodes
    {
        public const string InvalidMonth = "GetBookingCalendarQuery.InvalidMonth";
    }
}
