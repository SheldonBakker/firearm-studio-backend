namespace FirearmStudio.Application.Bookings;

public static class AvailabilityCalculator
{
    public readonly record struct BookedWindow(TimeOnly Start, TimeOnly End);

    public static IReadOnlyList<AvailabilitySlotDto> GetDaySlots(
        TimeOnly openTime,
        TimeOnly closeTime,
        int slotIntervalMinutes,
        int durationMinutes,
        int laneCount,
        TimeOnly earliestStart,
        IReadOnlyList<BookedWindow> bookings)
    {
        var slots = new List<AvailabilitySlotDto>();

        foreach (var start in CandidateStarts(openTime, closeTime, slotIntervalMinutes, durationMinutes))
        {
            if (start < earliestStart)
            {
                continue;
            }

            var end = start.AddMinutes(durationMinutes);
            var overlapping = bookings.Count(b => b.Start < end && b.End > start);
            if (overlapping < laneCount)
            {
                slots.Add(new AvailabilitySlotDto(start, end, laneCount - overlapping));
            }
        }

        return slots;
    }

    public static bool HasAnySlot(
        TimeOnly openTime,
        TimeOnly closeTime,
        int slotIntervalMinutes,
        int durationMinutes,
        int laneCount,
        TimeOnly earliestStart,
        IReadOnlyList<BookedWindow> bookings)
    {
        foreach (var start in CandidateStarts(openTime, closeTime, slotIntervalMinutes, durationMinutes))
        {
            if (start < earliestStart)
            {
                continue;
            }

            var end = start.AddMinutes(durationMinutes);
            if (bookings.Count(b => b.Start < end && b.End > start) < laneCount)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsOnSlotGrid(TimeOnly openTime, TimeOnly startTime, int slotIntervalMinutes)
    {
        var offset = (startTime - openTime).TotalMinutes;
        return offset >= 0 && offset % slotIntervalMinutes == 0;
    }

    private static IEnumerable<TimeOnly> CandidateStarts(
        TimeOnly openTime, TimeOnly closeTime, int slotIntervalMinutes, int durationMinutes)
    {
        var open = (int)openTime.ToTimeSpan().TotalMinutes;
        var close = (int)closeTime.ToTimeSpan().TotalMinutes;

        for (var start = open; start + durationMinutes <= close; start += slotIntervalMinutes)
        {
            yield return TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(start));
        }
    }
}
