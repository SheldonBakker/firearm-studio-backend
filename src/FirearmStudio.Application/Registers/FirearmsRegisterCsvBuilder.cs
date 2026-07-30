using System.Text;
using FirearmStudio.Application.Common;

namespace FirearmStudio.Application.Registers;

/// <summary>
/// Pure builder for the Firearms Register (FCA Regulation 37 stock-register style). No database
/// or file I/O; callers pre-load and pre-decrypt the rows.
/// </summary>
public static class FirearmsRegisterCsvBuilder
{
    public static readonly string[] Headers =
    [
        "Internal Ref",
        "Type",
        "Make",
        "Model",
        "Calibre",
        "Serial Number",
        "Owner Name",
        "Owner ID / Reg No",
        "Owner Address",
        "Licence Number",
        "Licence Issued",
        "Licence Expires",
        "Date Received",
        "Date Returned",
        "Firearm Status",
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
        row.InternalReference ?? string.Empty,
        row.FirearmType ?? string.Empty,
        row.Make,
        row.Model ?? string.Empty,
        row.Calibre ?? string.Empty,
        row.SerialNumber,
        row.OwnerName,
        row.OwnerIdOrRegNumber,
        row.OwnerAddress,
        row.LicenceNumber ?? string.Empty,
        RegisterFormatting.Date(row.LicenceIssuedOn),
        RegisterFormatting.Date(row.LicenceExpiresOn),
        RegisterFormatting.Date(row.StoredFrom),
        RegisterFormatting.Date(row.StoredUntil),
        row.FirearmStatus.ToString(),
    ];
}
