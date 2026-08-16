using FirearmStudio.Application.Common;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class BusinessDateTests
{
    [Fact]
    public void FromUtc_returns_sast_date_not_utc_date_in_early_morning_window()
    {
        var utc = new DateTime(2026, 8, 15, 22, 0, 0, DateTimeKind.Utc);

        var saDate = BusinessDate.FromUtc(utc);
        var utcDate = DateOnly.FromDateTime(utc.Date);

        Assert.Equal(new DateOnly(2026, 8, 16), saDate);
        Assert.Equal(new DateOnly(2026, 8, 15), utcDate);
        Assert.NotEqual(utcDate, saDate);
    }

    [Fact]
    public void FromUtc_at_one_second_before_sa_midnight_returns_the_current_utc_date()
    {
        var utc = new DateTime(2026, 8, 15, 21, 59, 59, DateTimeKind.Utc);

        var saDate = BusinessDate.FromUtc(utc);

        Assert.Equal(new DateOnly(2026, 8, 15), saDate);
    }

    [Fact]
    public void FromUtc_at_exactly_sa_midnight_advances_the_date()
    {
        var utc = new DateTime(2026, 8, 15, 22, 0, 0, DateTimeKind.Utc);

        var saDate = BusinessDate.FromUtc(utc);

        Assert.Equal(new DateOnly(2026, 8, 16), saDate);
    }

    [Fact]
    public void Today_is_consistent_with_africa_johannesburg_time_zone()
    {
        var expectedFromManualConvert = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SouthAfricaTimeZone.Instance));

        var actual = BusinessDate.Today();

        Assert.Equal(expectedFromManualConvert, actual);
    }
}
