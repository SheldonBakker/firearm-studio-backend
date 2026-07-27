using System.Globalization;
using System.Text;

namespace FirearmStudio.Application.Bookings;

/// <summary>
/// Pure builder for the attendance register CSV export. No database or file I/O; callers
/// pre-load the rows. Quoting follows RFC 4180: any field containing a comma, double quote,
/// carriage return, or line feed is wrapped in double quotes, and embedded double quotes are
/// doubled.
/// </summary>
public static class RegisterCsvBuilder
{
    private static readonly string[] Headers =
    [
        "Date",
        "Start Time",
        "End Time",
        "Range",
        "Booking Number",
        "Customer Name",
        "Attendee Name",
        "ID Number",
        "Licence Number",
        "Firearm Make/Model",
        "Serial Number",
        "Calibre",
        "Origin",
        "Signed Indemnity",
        "Checked In At",
    ];

    public static byte[] Build(IReadOnlyList<RegisterRowDto> rows)
    {
        var builder = new StringBuilder();

        WriteRow(builder, Headers);

        foreach (var row in rows)
        {
            WriteRow(builder, FormatRow(row));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string[] FormatRow(RegisterRowDto row) =>
    [
        row.BookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        row.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
        row.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
        row.RangeName,
        row.BookingNumber,
        row.CustomerName ?? string.Empty,
        row.AttendeeFullName,
        row.AttendeeIdNumber,
        row.LicenceNumber ?? string.Empty,
        row.FirearmMakeModel ?? string.Empty,
        row.FirearmSerialNumber ?? string.Empty,
        row.Calibre ?? string.Empty,
        row.FirearmOrigin.ToString(),
        row.SignedIndemnity ? "Yes" : "No",
        row.CheckedInAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty,
    ];

    private static void WriteRow(StringBuilder builder, string[] fields)
    {
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(QuoteField(fields[i]));
        }

        builder.Append("\r\n");
    }

    private static string QuoteField(string value)
    {
        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
