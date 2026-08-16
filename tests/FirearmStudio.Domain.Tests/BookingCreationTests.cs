using FirearmStudio.Application.Bookings;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class BookingCreationTests
{
    private static readonly Guid RangeId = Guid.NewGuid();
    private static readonly Guid PackageId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static BookingCreation.RangeData DefaultRange(DayOfWeek day = DayOfWeek.Monday) =>
        new(
            "Test Range",
            LaneCount: 2,
            SlotIntervalMinutes: 30,
            [new BookingCreation.OperatingHoursEntry(day, new TimeOnly(8, 0), new TimeOnly(17, 0))]);

    private static BookingCreation.PackageData DefaultPackage() =>
        new("Standard", 100m, 60, 4, []);

    private static BookingCreation.SlotRequest SlotRequest(DateOnly date, TimeOnly startTime, DayOfWeek day = DayOfWeek.Monday) =>
        new(RangeId, PackageId, CustomerId, date, startTime, 1, null, BookingSource.Public);

    [Fact]
    public void CreateBooking_today_before_cutoff_returns_StartTimeTooSoon()
    {
        var nowSast = new DateTime(2026, 8, 17, 10, 0, 0);
        var today = new DateOnly(2026, 8, 17);
        var startTime = new TimeOnly(10, 0);

        var result = BookingCreation.CreateBooking(
            SlotRequest(today, startTime, DayOfWeek.Monday),
            DefaultRange(DayOfWeek.Monday),
            DefaultPackage(),
            [],
            [],
            1,
            nowSast);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == BookingCreation.ErrorCodes.StartTimeTooSoon);
    }

    [Fact]
    public void CreateBooking_today_exactly_at_cutoff_on_grid_succeeds()
    {
        var nowSast = new DateTime(2026, 8, 17, 9, 30, 0);
        var today = new DateOnly(2026, 8, 17);
        var startTime = new TimeOnly(10, 0);

        var result = BookingCreation.CreateBooking(
            SlotRequest(today, startTime, DayOfWeek.Monday),
            DefaultRange(DayOfWeek.Monday),
            DefaultPackage(),
            [],
            [],
            1,
            nowSast);

        Assert.False(result.IsError);
    }

    [Fact]
    public void CreateBooking_future_date_early_morning_time_succeeds()
    {
        var nowSast = new DateTime(2026, 8, 17, 14, 0, 0);
        var futureDate = new DateOnly(2026, 8, 20);
        var startTime = new TimeOnly(8, 0);

        var result = BookingCreation.CreateBooking(
            SlotRequest(futureDate, startTime, DayOfWeek.Thursday),
            DefaultRange(DayOfWeek.Thursday),
            DefaultPackage(),
            [],
            [],
            1,
            nowSast);

        Assert.False(result.IsError);
    }

    [Fact]
    public void CreateBooking_past_date_returns_DateInPast_with_original_message()
    {
        var nowSast = new DateTime(2026, 8, 17, 10, 0, 0);
        var pastDate = new DateOnly(2026, 8, 10);
        var startTime = new TimeOnly(9, 0);

        var result = BookingCreation.CreateBooking(
            SlotRequest(pastDate, startTime, DayOfWeek.Sunday),
            DefaultRange(DayOfWeek.Sunday),
            DefaultPackage(),
            [],
            [],
            1,
            nowSast);

        Assert.True(result.IsError);
        var error = Assert.Single(result.Errors);
        Assert.Equal(BookingCreation.ErrorCodes.DateInPast, error.Code);
        Assert.Equal("Booking date may not be in the past.", error.Description);
    }
}
