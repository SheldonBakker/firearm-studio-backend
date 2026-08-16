using System.Text;
using FirearmStudio.Application.Registers;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class FirearmsRegisterCsvBuilderTests
{
    private static StorageRegisterRow SampleRow() => new()
    {
        InternalReference = "FA-001",
        FirearmType = "Pistol",
        Make = "CZ",
        Model = "75",
        Calibre = "9mmP",
        SerialNumber = "SN123",
        FirearmStatus = FirearmStatus.InStorage,
        OwnerType = CustomerType.Individual,
        OwnerFullName = "Jane Dlamini",
        OwnerIdNumber = "8501015800081",
        AddressLine1 = "1 Main Rd",
        City = "Cape Town",
        Province = "Western Cape",
        PostalCode = "8001",
        LicenceNumber = "L-456",
        LicenceIssuedOn = new DateOnly(2022, 3, 1),
        LicenceExpiresOn = new DateOnly(2027, 3, 1),
        StoredFrom = new DateOnly(2026, 2, 1),
        StoredUntil = null,
        StorageStatus = StorageStatus.Active,
    };

    private static string BuildText(params StorageRegisterRow[] rows) =>
        Encoding.UTF8.GetString(FirearmsRegisterCsvBuilder.Build(rows));

    [Fact]
    public void Build_with_no_rows_returns_header_row_only()
    {
        var text = BuildText();

        Assert.Equal(
            "Internal Ref,Type,Make,Model,Calibre,Serial Number,Owner Name,Owner ID / Reg No," +
            "Owner Address,Licence Number,Licence Issued,Licence Expires,Date Received," +
            "Date Returned,Firearm Status\r\n",
            text);
    }

    [Fact]
    public void FormatRow_maps_columns_in_header_order()
    {
        var fields = FirearmsRegisterCsvBuilder.FormatRow(SampleRow());

        Assert.Equal(FirearmsRegisterCsvBuilder.Headers.Length, fields.Length);
        Assert.Equal("FA-001", fields[0]);
        Assert.Equal("Pistol", fields[1]);
        Assert.Equal("CZ", fields[2]);
        Assert.Equal("75", fields[3]);
        Assert.Equal("9mmP", fields[4]);
        Assert.Equal("SN123", fields[5]);
        Assert.Equal("Jane Dlamini", fields[6]);
        Assert.Equal("8501015800081", fields[7]);
        Assert.Equal("1 Main Rd, Cape Town, Western Cape, 8001", fields[8]);
        Assert.Equal("L-456", fields[9]);
        Assert.Equal("2022-03-01", fields[10]);
        Assert.Equal("2027-03-01", fields[11]);
        Assert.Equal("2026-02-01", fields[12]);
        Assert.Equal(string.Empty, fields[13]);
        Assert.Equal("InStorage", fields[14]);
    }

    [Fact]
    public void Build_neutralizes_formula_triggering_cells()
    {
        var row = SampleRow() with { OwnerFullName = "=SUM(A1:A9)" };

        var text = BuildText(row);

        Assert.Contains("'=SUM(A1:A9)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_quotes_fields_containing_commas()
    {
        var text = BuildText(SampleRow());

        Assert.Contains("\"1 Main Rd, Cape Town, Western Cape, 8001\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_never_emits_licence_placeholders_for_unlicensed_firearms()
    {
        var row = SampleRow() with { LicenceNumber = null, LicenceIssuedOn = null, LicenceExpiresOn = null };

        var fields = FirearmsRegisterCsvBuilder.FormatRow(row);

        Assert.Equal(string.Empty, fields[9]);
        Assert.Equal(string.Empty, fields[10]);
        Assert.Equal(string.Empty, fields[11]);
    }
}
