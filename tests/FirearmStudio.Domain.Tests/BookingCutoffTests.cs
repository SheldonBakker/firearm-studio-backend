using FirearmStudio.Application.Bookings;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class BookingCutoffTests
{
    [Fact]
    public void EarliestStart_future_date_returns_MinValue()
    {
        var nowSast = new DateTime(2026, 8, 16, 10, 0, 0);
        var futureDate = new DateOnly(2026, 8, 20);

        var result = BookingCutoff.EarliestStart(futureDate, nowSast);

        Assert.Equal(TimeOnly.MinValue, result);
    }

    [Fact]
    public void EarliestStart_past_date_returns_null()
    {
        var nowSast = new DateTime(2026, 8, 16, 10, 0, 0);
        var pastDate = new DateOnly(2026, 8, 10);

        var result = BookingCutoff.EarliestStart(pastDate, nowSast);

        Assert.Null(result);
    }

    [Fact]
    public void EarliestStart_today_at_10_00_returns_10_30()
    {
        var nowSast = new DateTime(2026, 8, 16, 10, 0, 0);
        var today = new DateOnly(2026, 8, 16);

        var result = BookingCutoff.EarliestStart(today, nowSast);

        Assert.Equal(new TimeOnly(10, 30), result);
    }

    [Fact]
    public void EarliestStart_today_at_23_50_returns_null_due_to_midnight_rollover()
    {
        var nowSast = new DateTime(2026, 8, 16, 23, 50, 0);
        var today = new DateOnly(2026, 8, 16);

        var result = BookingCutoff.EarliestStart(today, nowSast);

        Assert.Null(result);
    }

    [Fact]
    public void EarliestStart_next_day_when_now_is_23_50_returns_00_20()
    {
        var nowSast = new DateTime(2026, 8, 16, 23, 50, 0);
        var nextDay = new DateOnly(2026, 8, 17);

        var result = BookingCutoff.EarliestStart(nextDay, nowSast);

        Assert.Equal(new TimeOnly(0, 20), result);
    }
}
