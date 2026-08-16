using System.Globalization;
using FirearmStudio.Application.Common;

namespace FirearmStudio.Application.Bookings;

public static class BookingCalendarLinkBuilder
{
    private const string GoogleCalendarDateFormat = "yyyyMMddTHHmmss";

    public sealed record Links(string? IcsUrl, string? GoogleCalendarUrl);

    public static Links Build(
        string publicBaseUrl,
        string calendarToken,
        BookingIcsBuilder.BookingIcsData booking,
        BookingIcsBuilder.CompanyIcsData company)
    {
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            return new Links(null, null);
        }

        var icsUrl = $"{publicBaseUrl.TrimEnd('/')}/api/v1/public/bookings/{calendarToken}/calendar.ics";

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(
            booking.BookingDate.ToDateTime(booking.StartTime), SouthAfricaTimeZone.Instance);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(
            booking.BookingDate.ToDateTime(booking.EndTime), SouthAfricaTimeZone.Instance);

        var text = $"{booking.PackageName} - {booking.RangeName}";
        var details = $"Booking {booking.BookingNumber} for {booking.ShooterCount} shooter(s).";
        var location = BookingIcsBuilder.BuildLocation(company);

        var googleCalendarUrl = "https://calendar.google.com/calendar/render?action=TEMPLATE"
            + $"&text={Uri.EscapeDataString(text)}"
            + $"&dates={FormatUtc(startUtc)}/{FormatUtc(endUtc)}"
            + $"&details={Uri.EscapeDataString(details)}"
            + $"&location={Uri.EscapeDataString(location)}";

        return new Links(icsUrl, googleCalendarUrl);
    }

    private static string FormatUtc(DateTime utc) =>
        utc.ToString(GoogleCalendarDateFormat, CultureInfo.InvariantCulture) + "Z";
}
