using FirearmStudio.Application.Common;

namespace FirearmStudio.Application.Bookings;

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
