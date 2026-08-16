using System.Globalization;
using System.Text;
using FirearmStudio.Application.Common;

namespace FirearmStudio.Application.Bookings;

public static class BookingIcsBuilder
{
    private const string DateTimeFormat = "yyyyMMddTHHmmss";

    public sealed record BookingIcsData(
        Guid BookingId,
        string BookingNumber,
        string PackageName,
        string RangeName,
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
        var summary = $"{booking.PackageName} - {booking.RangeName}";
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
            $"SUMMARY:{EscapeRfc5545Text(summary)}",
            $"LOCATION:{EscapeRfc5545Text(location)}",
            $"DESCRIPTION:{EscapeRfc5545Text(description)}",
            "END:VEVENT",
            "END:VCALENDAR",
        };

        var text = string.Join("\r\n", lines) + "\r\n";
        return Encoding.UTF8.GetBytes(text);
    }

    internal static string BuildLocation(CompanyIcsData company)
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

    private static string EscapeRfc5545Text(string value)
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
