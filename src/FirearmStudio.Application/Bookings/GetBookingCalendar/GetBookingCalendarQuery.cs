using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.GetBookingCalendar;

public sealed record GetBookingCalendarQuery(
    int Year,
    int Month,
    Guid? ShootingRangeId) : IQuery<ErrorOr<IReadOnlyList<BookingCalendarItemDto>>>;
