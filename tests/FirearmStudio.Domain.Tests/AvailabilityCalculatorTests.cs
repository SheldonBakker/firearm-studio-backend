using FirearmStudio.Application.Bookings;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class AvailabilityCalculatorTests
{
    private static readonly TimeOnly Open = new(8, 0);
    private static readonly TimeOnly Close = new(17, 0);
    private const int Interval = 30;
    private const int Duration = 60;
    private const int Lanes = 2;

    [Fact]
    public void GetDaySlots_with_MinValue_earliestStart_returns_all_open_slots()
    {
        var slots = AvailabilityCalculator.GetDaySlots(
            Open, Close, Interval, Duration, Lanes,
            TimeOnly.MinValue,
            []);

        Assert.Equal(17, slots.Count);
        Assert.Equal(Open, slots[0].StartTime);
        Assert.Equal(new TimeOnly(16, 0), slots[^1].StartTime);
    }

    [Fact]
    public void GetDaySlots_with_midday_earliestStart_drops_earlier_slots_keeps_later()
    {
        var earliestStart = new TimeOnly(12, 0);

        var slots = AvailabilityCalculator.GetDaySlots(
            Open, Close, Interval, Duration, Lanes,
            earliestStart,
            []);

        Assert.All(slots, s => Assert.True(s.StartTime >= earliestStart));
        Assert.True(slots.Count > 0);
        Assert.Equal(new TimeOnly(12, 0), slots[0].StartTime);
    }

    [Fact]
    public void GetDaySlots_slot_starting_exactly_at_earliestStart_is_included()
    {
        var earliestStart = new TimeOnly(10, 0);

        var slots = AvailabilityCalculator.GetDaySlots(
            Open, Close, Interval, Duration, Lanes,
            earliestStart,
            []);

        Assert.Contains(slots, s => s.StartTime == earliestStart);
    }

    [Fact]
    public void GetDaySlots_earliestStart_after_closeTime_returns_empty()
    {
        var earliestStart = new TimeOnly(18, 0);

        var slots = AvailabilityCalculator.GetDaySlots(
            Open, Close, Interval, Duration, Lanes,
            earliestStart,
            []);

        Assert.Empty(slots);
    }

    [Fact]
    public void GetDaySlots_lane_and_overlap_behaviour_still_works_with_cutoff()
    {
        var earliestStart = new TimeOnly(10, 0);
        var bookings = new[]
        {
            new AvailabilityCalculator.BookedWindow(new TimeOnly(10, 0), new TimeOnly(11, 0)),
            new AvailabilityCalculator.BookedWindow(new TimeOnly(10, 0), new TimeOnly(11, 0)),
        };

        var slots = AvailabilityCalculator.GetDaySlots(
            Open, Close, Interval, Duration, Lanes,
            earliestStart,
            bookings);

        Assert.DoesNotContain(slots, s => s.StartTime == new TimeOnly(10, 0));
        Assert.Contains(slots, s => s.StartTime == new TimeOnly(11, 0));
    }

    [Fact]
    public void HasAnySlot_with_cutoff_filters_early_slots_returns_false_when_all_past_close()
    {
        var hasSlot = AvailabilityCalculator.HasAnySlot(
            Open, Close, Interval, Duration, Lanes,
            new TimeOnly(18, 0),
            []);

        Assert.False(hasSlot);
    }

    [Fact]
    public void HasAnySlot_with_cutoff_returns_true_when_remaining_slots_have_capacity()
    {
        var hasSlot = AvailabilityCalculator.HasAnySlot(
            Open, Close, Interval, Duration, Lanes,
            new TimeOnly(12, 0),
            []);

        Assert.True(hasSlot);
    }
}
