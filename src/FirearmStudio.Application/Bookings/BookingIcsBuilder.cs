using System.Globalization;
using System.Text;
using FirearmStudio.Application.Common;

namespace FirearmStudio.Application.Bookings;

/// <summary>
/// Pure builder for the RFC 5545 (iCalendar) file backing a booking's "add to calendar" link.
/// No database or file I/O; callers pre-load whatever data the fields below need.
/// </summary>
public static class BookingIcsBuilder
{
    private const string DateTimeFormat = "yyyyMMddTHHmmss";

    public sealed record BookingIcsData(
        Guid BookingId,
        string BookingNumber,
        string PackageName,
        DateOnly BookingDate,
        TimeOnly StartTime,
        TimeOnly EndTime,
        int ShooterCount);

    public sealed record CompanyIcsData(
        string Name,
        string? AddressLine1,
        string? AddressLine2,
        string? City,
        string? Province,
        string? PostalCode);

    public static byte[] Build(BookingIcsData booking, CompanyIcsData company, DateTime utcNow)
    {
        var start = booking.BookingDate.ToDateTime(booking.StartTime);
        var end = booking.BookingDate.ToDateTime(booking.EndTime);
        var location = BuildLocation(company);
        var description = $"Booking {booking.BookingNumber} for {booking.ShooterCount} shooter(s).";

        var lines = new[]
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//FirearmStudio//Booking Calendar//EN",
            "CALSCALE:GREGORIAN",
            "BEGIN:VEVENT",
            $"UID:{booking.BookingId}",
            $"DTSTAMP:{utcNow.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}Z",
            $"DTSTART;TZID={SouthAfricaTimeZone.Instance.Id}:{start.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}",
            $"DTEND;TZID={SouthAfricaTimeZone.Instance.Id}:{end.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}",
            $"SUMMARY:{EscapeText(booking.PackageName)}",
            $"LOCATION:{EscapeText(location)}",
            $"DESCRIPTION:{EscapeText(description)}",
            "END:VEVENT",
            "END:VCALENDAR",
        };

        var text = string.Join("\r\n", lines) + "\r\n";
        return Encoding.UTF8.GetBytes(text);
    }

    private static string BuildLocation(CompanyIcsData company)
    {
        var parts = new[]
        {
            company.AddressLine1,
            company.AddressLine2,
            company.City,
            company.Province,
            company.PostalCode,
        };

        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    /// <summary>RFC 5545 §3.3.11 TEXT escaping: backslash, comma, semicolon, and newline.</summary>
    private static string EscapeText(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case ';':
                    builder.Append("\\;");
                    break;
                case ',':
                    builder.Append("\\,");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }
}
