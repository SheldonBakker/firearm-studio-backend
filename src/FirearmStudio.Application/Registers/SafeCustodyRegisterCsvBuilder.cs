using System.Text;
using FirearmStudio.Application.Common;

namespace FirearmStudio.Application.Registers;

public static class SafeCustodyRegisterCsvBuilder
{
    public static readonly string[] Headers =
    [
        "Date Received",
        "Date Returned",
        "Make",
        "Model",
        "Calibre",
        "Serial Number",
        "Licence Holder",
        "ID / Reg No",
        "Address",
        "Licence Number",
        "Licence Issued",
        "Safe Number",
        "Rack Number",
        "Storage Location",
        "Storage Status",
    ];

    public static byte[] Build(IReadOnlyList<StorageRegisterRow> rows)
    {
        var builder = new StringBuilder();

        CsvWriting.WriteRow(builder, Headers);

        foreach (var row in rows)
        {
            CsvWriting.WriteRow(builder, FormatRow(row));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public static string[] FormatRow(StorageRegisterRow row) =>
    [
        RegisterFormatting.Date(row.StoredFrom),
        RegisterFormatting.Date(row.StoredUntil),
        row.Make,
        row.Model ?? string.Empty,
        row.Calibre ?? string.Empty,
        row.SerialNumber,
        row.OwnerName,
        row.OwnerIdOrRegNumber,
        row.OwnerAddress,
        row.LicenceNumber ?? string.Empty,
        RegisterFormatting.Date(row.LicenceIssuedOn),
        row.SafeNumber ?? string.Empty,
        row.RackNumber ?? string.Empty,
        row.StorageLocation ?? string.Empty,
        row.StorageStatus.ToString(),
    ];
}
