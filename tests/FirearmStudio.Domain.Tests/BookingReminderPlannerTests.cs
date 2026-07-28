using FirearmStudio.Application.Bookings;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class BookingReminderPlannerTests
{
    // 2026-08-01 09:00 Africa/Johannesburg (UTC+2, no DST) is 2026-08-01 07:00 UTC.
    private static readonly DateOnly BookingDate = new(2026, 8, 1);
    private static readonly TimeOnly StartTime = new(9, 0);
    private static readonly DateTime SessionStartUtc = new(2026, 8, 1, 7, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsReminderDue_returns_true_exactly_24_hours_before_session_start()
    {
        var nowUtc = SessionStartUtc.AddHours(-24);

        Assert.True(BookingReminderPlanner.IsReminderDue(nowUtc, BookingDate, StartTime));
    }

    [Fact]
    public void IsReminderDue_returns_false_just_before_the_24_hour_window_opens()
    {
        var nowUtc = SessionStartUtc.AddHours(-24).AddSeconds(-1);

        Assert.False(BookingReminderPlanner.IsReminderDue(nowUtc, BookingDate, StartTime));
    }

    [Fact]
    public void IsReminderDue_returns_false_exactly_at_session_start()
    {
        Assert.False(BookingReminderPlanner.IsReminderDue(SessionStartUtc, BookingDate, StartTime));
    }

    [Fact]
    public void IsReminderDue_returns_false_once_the_session_has_already_started()
    {
        var nowUtc = SessionStartUtc.AddMinutes(1);

        Assert.False(BookingReminderPlanner.IsReminderDue(nowUtc, BookingDate, StartTime));
    }

    [Fact]
    public void IsReminderDue_returns_true_in_the_middle_of_the_window()
    {
        var nowUtc = SessionStartUtc.AddHours(-12);

        Assert.True(BookingReminderPlanner.IsReminderDue(nowUtc, BookingDate, StartTime));
    }

    [Fact]
    public void IsReminderDue_handles_local_midnight_crossing_into_the_previous_utc_day()
    {
        // 2026-08-01 00:30 Africa/Johannesburg (local booking date) is 2026-07-31 22:30 UTC:
        // the UTC day is one behind the local booking date.
        var bookingDate = new DateOnly(2026, 8, 1);
        var startTime = new TimeOnly(0, 30);
        var sessionStartUtc = new DateTime(2026, 7, 31, 22, 30, 0, DateTimeKind.Utc);

        Assert.True(BookingReminderPlanner.IsReminderDue(sessionStartUtc.AddHours(-24), bookingDate, startTime));
        Assert.True(BookingReminderPlanner.IsReminderDue(sessionStartUtc.AddHours(-1), bookingDate, startTime));
        Assert.False(BookingReminderPlanner.IsReminderDue(sessionStartUtc, bookingDate, startTime));
    }
}
