using FirearmStudio.Application.Common;

namespace FirearmStudio.Application.Bookings;

/// <summary>
/// Pure scheduling rule for booking reminders: a reminder is due once <c>nowUtc</c> falls in the
/// 24 hours immediately before the session start and stops being due once the session has begun.
/// The booking's date and start time are local Africa/Johannesburg values; they are converted to
/// UTC via <see cref="SouthAfricaTimeZone"/> before comparing against <c>nowUtc</c>, so a booking
/// starting shortly after local midnight still resolves against the correct UTC day.
/// </summary>
public static class BookingReminderPlanner
{
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(24);

    public static bool IsReminderDue(DateTime nowUtc, DateOnly bookingDate, TimeOnly startTime)
    {
        var sessionStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            bookingDate.ToDateTime(startTime), SouthAfricaTimeZone.Instance);

        var windowStart = sessionStartUtc - ReminderWindow;

        return nowUtc >= windowStart && nowUtc < sessionStartUtc;
    }
}
