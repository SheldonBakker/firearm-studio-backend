namespace FirearmStudio.Application.Common;

public static class BusinessDate
{
    public static DateOnly Today() => FromUtc(DateTime.UtcNow);

    public static DateOnly FromUtc(DateTime utcNow) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcNow, SouthAfricaTimeZone.Instance));
}
