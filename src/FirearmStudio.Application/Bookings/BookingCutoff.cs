namespace FirearmStudio.Application.Bookings;

public static class BookingCutoff
{
    public const int LeadTimeMinutes = 30;

    public static TimeOnly? EarliestStart(DateOnly date, DateTime nowSast)
    {
        var cutoff = nowSast.AddMinutes(LeadTimeMinutes);
        var cutoffDate = DateOnly.FromDateTime(cutoff);
        if (date > cutoffDate)
        {
            return TimeOnly.MinValue;
        }

        if (date < cutoffDate)
        {
            return null;
        }

        return TimeOnly.FromDateTime(cutoff);
    }
}
