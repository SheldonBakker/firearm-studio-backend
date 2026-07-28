using System.Globalization;
using FirearmStudio.Application.Common;

namespace FirearmStudio.Application.Bookings;

/// <summary>
/// Builds the "add to calendar" links attached to booking lifecycle notifications: the public
/// .ics download URL (served by the public calendar endpoint) and a prefilled Google Calendar
/// event URL. Both derive from the same booking/company data as <see cref="BookingIcsBuilder"/>,
/// converting the booking's Africa/Johannesburg local start/end into UTC via the shared
/// <see cref="SouthAfricaTimeZone"/>, so the two links stay in sync.
/// </summary>
public static class BookingCalendarLinkBuilder
{
    private const string GoogleCalendarDateFormat = "yyyyMMddTHHmmss";

    public sealed record Links(string? IcsUrl, string? GoogleCalendarUrl);

    /// <summary>
    /// Returns null links when <paramref name="publicBaseUrl"/> is empty, rather than emitting
    /// broken relative URLs.
    /// </summary>
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
