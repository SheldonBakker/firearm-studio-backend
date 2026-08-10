using FirearmStudio.Application.Registers;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class StorageRegisterRowTests
{
    private static StorageRecord SampleRecord() => new()
    {
        StoredFrom = new DateOnly(2026, 2, 1),
        StoredUntil = null,
        StorageStatus = StorageStatus.Active,
        StorageLocation = "Main safe room",
        RackNumber = "R4",
        SafeNumber = "S12",
        Firearm = new Firearm
        {
            Make = "CZ",
            Model = "75",
            Calibre = "9mmP",
            FirearmType = "Pistol",
            SerialNumber = "SN123",
            InternalReference = "FA-001",
            Status = FirearmStatus.InStorage,
            Customer = new Customer
            {
                CustomerType = CustomerType.Individual,
                FullName = "Jane Dlamini",
                IdNumber = "8501015800081",
                AddressLine1 = "1 Main Rd",
                AddressLine2 = null,
                City = "Cape Town",
                Province = "Western Cape",
                PostalCode = "8001",
            },
            Licences =
            [
                new FirearmLicence { LicenceNumber = "OLD-123", ExpiresOn = new DateOnly(2025, 1, 1) },
                new FirearmLicence
                {
                    LicenceNumber = "NEW-456",
                    IssuedOn = new DateOnly(2022, 3, 1),
                    ExpiresOn = new DateOnly(2027, 3, 1),
                },
            ],
        },
    };

    private static StorageRegisterRow Project(StorageRecord record) =>
        StorageRegisterRow.QueryProjection.Compile()(record);

    [Fact]
    public void QueryProjection_maps_firearm_and_storage_fields()
    {
        var row = Project(SampleRecord());

        Assert.Equal("CZ", row.Make);
        Assert.Equal("SN123", row.SerialNumber);
        Assert.Equal("Pistol", row.FirearmType);
        Assert.Equal(new DateOnly(2026, 2, 1), row.StoredFrom);
        Assert.Null(row.StoredUntil);
        Assert.Equal("S12", row.SafeNumber);
        Assert.Equal(FirearmStatus.InStorage, row.FirearmStatus);
        Assert.Equal(StorageStatus.Active, row.StorageStatus);
    }

    [Fact]
    public void QueryProjection_selects_licence_with_latest_expiry()
    {
        var row = Project(SampleRecord());

        Assert.Equal("NEW-456", row.LicenceNumber);
        Assert.Equal(new DateOnly(2022, 3, 1), row.LicenceIssuedOn);
        Assert.Equal(new DateOnly(2027, 3, 1), row.LicenceExpiresOn);
    }

    [Fact]
    public void QueryProjection_with_no_licences_leaves_licence_columns_null()
    {
        var record = SampleRecord();
        record.Firearm!.Licences = [];

        var row = Project(record);

        Assert.Null(row.LicenceNumber);
        Assert.Null(row.LicenceIssuedOn);
        Assert.Null(row.LicenceExpiresOn);
    }

    [Fact]
    public void QueryProjection_maps_the_owner_id_number()
    {
        var row = Project(SampleRecord());

        Assert.Equal("8501015800081", row.OwnerIdNumber);
    }

    [Fact]
    public void QueryProjection_leaves_the_owner_id_number_null_when_the_customer_has_none()
    {
        var record = SampleRecord();
        record.Firearm!.Customer!.IdNumber = null;

        var row = Project(record);

        Assert.Null(row.OwnerIdNumber);
        Assert.Equal(string.Empty, row.OwnerIdOrRegNumber);
    }

    [Fact]
    public void OwnerName_uses_full_name_for_individuals()
    {
        var row = Project(SampleRecord());
        Assert.Equal("Jane Dlamini", row.OwnerName);
    }

    [Fact]
    public void OwnerName_and_id_use_company_fields_for_company_customers()
    {
        var record = SampleRecord();
        record.Firearm!.Customer!.CustomerType = CustomerType.Company;
        record.Firearm.Customer.CompanyName = "Acme Security (Pty) Ltd";
        record.Firearm.Customer.RegistrationNumber = "2019/123456/07";

        var row = Project(record);

        Assert.Equal("Acme Security (Pty) Ltd", row.OwnerName);
        Assert.Equal("2019/123456/07", row.OwnerIdOrRegNumber);
    }

    [Fact]
    public void OwnerIdOrRegNumber_uses_the_id_number_for_individuals()
    {
        var row = Project(SampleRecord());
        Assert.Equal("8501015800081", row.OwnerIdOrRegNumber);
    }

    [Fact]
    public void OwnerAddress_joins_non_empty_parts_with_commas()
    {
        var row = Project(SampleRecord());
        Assert.Equal("1 Main Rd, Cape Town, Western Cape, 8001", row.OwnerAddress);
    }
}
