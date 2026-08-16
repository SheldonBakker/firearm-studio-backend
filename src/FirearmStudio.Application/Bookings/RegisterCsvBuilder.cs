using System.Globalization;
using System.Text;
using FirearmStudio.Application.Common;

namespace FirearmStudio.Application.Bookings;

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

        CsvWriting.WriteRow(builder, Headers);

        foreach (var row in rows)
        {
            CsvWriting.WriteRow(builder, FormatRow(row));
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
}
