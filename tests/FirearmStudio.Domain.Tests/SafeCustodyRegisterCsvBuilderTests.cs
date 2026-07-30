using System.Text;
using FirearmStudio.Application.Registers;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class SafeCustodyRegisterCsvBuilderTests
{
    private static StorageRegisterRow SampleRow() => new()
    {
        Make = "CZ",
        Model = "75",
        Calibre = "9mmP",
        SerialNumber = "SN123",
        OwnerType = CustomerType.Individual,
        OwnerFullName = "Jane Dlamini",
        OwnerIdNumber = "8501015800081",
        AddressLine1 = "1 Main Rd",
        City = "Cape Town",
        Province = "Western Cape",
        PostalCode = "8001",
        LicenceNumber = "L-456",
        LicenceIssuedOn = new DateOnly(2022, 3, 1),
        StoredFrom = new DateOnly(2026, 2, 1),
        StoredUntil = new DateOnly(2026, 5, 15),
        StorageStatus = StorageStatus.Released,
        StorageLocation = "Main safe room",
        RackNumber = "R4",
        SafeNumber = "S12",
    };

    [Fact]
    public void Build_with_no_rows_returns_header_row_only()
    {
        var text = Encoding.UTF8.GetString(SafeCustodyRegisterCsvBuilder.Build([]));

        Assert.Equal(
            "Date Received,Date Returned,Make,Model,Calibre,Serial Number,Licence Holder," +
            "ID / Reg No,Address,Licence Number,Licence Issued,Safe Number,Rack Number," +
            "Storage Location,Storage Status\r\n",
            text);
    }

    [Fact]
    public void FormatRow_maps_columns_in_header_order()
    {
        var fields = SafeCustodyRegisterCsvBuilder.FormatRow(SampleRow());

        Assert.Equal(SafeCustodyRegisterCsvBuilder.Headers.Length, fields.Length);
        Assert.Equal("2026-02-01", fields[0]);
        Assert.Equal("2026-05-15", fields[1]);
        Assert.Equal("CZ", fields[2]);
        Assert.Equal("75", fields[3]);
        Assert.Equal("9mmP", fields[4]);
        Assert.Equal("SN123", fields[5]);
        Assert.Equal("Jane Dlamini", fields[6]);
        Assert.Equal("8501015800081", fields[7]);
        Assert.Equal("1 Main Rd, Cape Town, Western Cape, 8001", fields[8]);
        Assert.Equal("L-456", fields[9]);
        Assert.Equal("2022-03-01", fields[10]);
        Assert.Equal("S12", fields[11]);
        Assert.Equal("R4", fields[12]);
        Assert.Equal("Main safe room", fields[13]);
        Assert.Equal("Released", fields[14]);
    }

    [Fact]
    public void Headers_contain_no_signature_column_in_csv()
    {
        Assert.DoesNotContain("Signature", SafeCustodyRegisterCsvBuilder.Headers);
    }

    [Fact]
    public void Build_neutralizes_formula_triggering_cells()
    {
        var row = SampleRow() with { StorageLocation = "@HYPERLINK(evil)" };

        var text = Encoding.UTF8.GetString(SafeCustodyRegisterCsvBuilder.Build([row]));

        Assert.Contains("'@HYPERLINK(evil)", text, StringComparison.Ordinal);
    }
}
