namespace FirearmStudio.Application.Common;

public static class BusinessDate
{
    public static DateOnly Today() => FromUtc(DateTime.UtcNow);

    public static DateOnly FromUtc(DateTime utcNow) =>
        DateOnly.FromDateTime(NowFromUtc(utcNow));

    public static DateTime Now() => NowFromUtc(DateTime.UtcNow);

    public static DateTime NowFromUtc(DateTime utcNow) =>
        TimeZoneInfo.ConvertTimeFromUtc(utcNow, SouthAfricaTimeZone.Instance);
}
