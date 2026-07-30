using FirearmStudio.Application.Registers;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class RegisterDocumentFactoryTests
{
    private static Company SampleCompany() => new()
    {
        Name = "Bergview Arms",
        RegistrationNumber = "2015/098765/07",
        AddressLine1 = "12 Range Rd",
        City = "Paarl",
        Province = "Western Cape",
        PostalCode = "7646",
    };

    private static StorageRegisterRow SampleRow() => new()
    {
        Make = "CZ",
        SerialNumber = "SN123",
        OwnerType = CustomerType.Individual,
        OwnerFullName = "Jane Dlamini",
        StoredFrom = new DateOnly(2026, 2, 1),
    };

    private static RegisterDocument Create(RegisterKind kind, params StorageRegisterRow[] rows) =>
        RegisterDocumentFactory.Create(
            kind,
            rows,
            SampleCompany(),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 6, 30),
            new DateTime(2026, 7, 29, 10, 0, 0, DateTimeKind.Utc),
            "sheldon@wbwr.io");

    [Fact]
    public void Create_firearms_register_uses_firearms_columns_and_title()
    {
        var document = Create(RegisterKind.Firearms, SampleRow());

        Assert.Equal("Firearms Register", document.Title);
        Assert.Equal(FirearmsRegisterCsvBuilder.Headers, document.Columns);
        Assert.Single(document.Rows);
        Assert.Equal(FirearmsRegisterCsvBuilder.Headers.Length, document.Rows[0].Length);
    }

    [Fact]
    public void Create_safe_custody_register_appends_blank_signature_column()
    {
        var document = Create(RegisterKind.SafeCustody, SampleRow());

        Assert.Equal("Safe Custody Register", document.Title);
        Assert.Equal("Signature", document.Columns[^1]);
        Assert.Equal(SafeCustodyRegisterCsvBuilder.Headers.Length + 1, document.Columns.Count);
        Assert.Equal(string.Empty, document.Rows[0][^1]);
    }

    [Fact]
    public void Create_composes_company_header_fields()
    {
        var document = Create(RegisterKind.Firearms);

        Assert.Equal("Bergview Arms", document.CompanyName);
        Assert.Equal("2015/098765/07", document.CompanyRegistrationNumber);
        Assert.Equal("12 Range Rd, Paarl, Western Cape, 7646", document.CompanyAddress);
        Assert.Equal(new DateOnly(2026, 1, 1), document.PeriodFrom);
        Assert.Equal(new DateOnly(2026, 6, 30), document.PeriodTo);
        Assert.Equal("sheldon@wbwr.io", document.GeneratedBy);
    }

    [Fact]
    public void Create_converts_generated_at_to_south_africa_time()
    {
        var document = Create(RegisterKind.Firearms);

        // 10:00 UTC is 12:00 SAST (UTC+2, no DST).
        Assert.Equal(new DateTime(2026, 7, 29, 12, 0, 0), document.GeneratedAt);
    }

    [Fact]
    public void Create_with_no_rows_sets_empty_state_text()
    {
        var document = Create(RegisterKind.SafeCustody);

        Assert.Empty(document.Rows);
        Assert.Equal("No movements in period.", document.EmptyStateText);
    }
}
