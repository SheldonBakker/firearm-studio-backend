using System.Linq.Expressions;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Registers;

/// <summary>
/// Flat row for register exports, one per storage record. Constructed inside an EF expression
/// tree, so no required members. The ID number travels as ciphertext; the export handler decrypts
/// it into <see cref="OwnerIdNumber"/> via a with-expression before rows reach a builder.
/// </summary>
public sealed record StorageRegisterRow
{
    public string? InternalReference { get; init; }
    public string? FirearmType { get; init; }
    public string Make { get; init; } = string.Empty;
    public string? Model { get; init; }
    public string? Calibre { get; init; }
    public string SerialNumber { get; init; } = string.Empty;
    public FirearmStatus FirearmStatus { get; init; }

    public CustomerType OwnerType { get; init; }
    public string? OwnerFullName { get; init; }
    public string? OwnerCompanyName { get; init; }
    public string? OwnerIdNumberCiphertext { get; init; }
    public string? OwnerRegistrationNumber { get; init; }
    public string? OwnerIdNumber { get; init; }

    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }

    public string? LicenceNumber { get; init; }
    public DateOnly? LicenceIssuedOn { get; init; }
    public DateOnly? LicenceExpiresOn { get; init; }

    public DateOnly StoredFrom { get; init; }
    public DateOnly? StoredUntil { get; init; }
    public StorageStatus StorageStatus { get; init; }
    public string? StorageLocation { get; init; }
    public string? RackNumber { get; init; }
    public string? SafeNumber { get; init; }

    public string OwnerName =>
        (OwnerType == CustomerType.Company ? OwnerCompanyName : OwnerFullName) ?? string.Empty;

    public string OwnerIdOrRegNumber =>
        (OwnerType == CustomerType.Company ? OwnerRegistrationNumber : OwnerIdNumber) ?? string.Empty;

    public string OwnerAddress => string.Join(", ",
        new[] { AddressLine1, AddressLine2, City, Province, PostalCode }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

    public static Expression<Func<StorageRecord, StorageRegisterRow>> QueryProjection => r => new StorageRegisterRow
    {
        InternalReference = r.Firearm!.InternalReference,
        FirearmType = r.Firearm.FirearmType,
        Make = r.Firearm.Make,
        Model = r.Firearm.Model,
        Calibre = r.Firearm.Calibre,
        SerialNumber = r.Firearm.SerialNumber,
        FirearmStatus = r.Firearm.Status,
        OwnerType = r.Firearm.Customer!.CustomerType,
        OwnerFullName = r.Firearm.Customer.FullName,
        OwnerCompanyName = r.Firearm.Customer.CompanyName,
        OwnerIdNumberCiphertext = r.Firearm.Customer.IdNumberCiphertext,
        OwnerRegistrationNumber = r.Firearm.Customer.RegistrationNumber,
        AddressLine1 = r.Firearm.Customer.AddressLine1,
        AddressLine2 = r.Firearm.Customer.AddressLine2,
        City = r.Firearm.Customer.City,
        Province = r.Firearm.Customer.Province,
        PostalCode = r.Firearm.Customer.PostalCode,
        LicenceNumber = r.Firearm.Licences
            .OrderByDescending(l => l.ExpiresOn)
            .Select(l => (string?)l.LicenceNumber)
            .FirstOrDefault(),
        LicenceIssuedOn = r.Firearm.Licences
            .OrderByDescending(l => l.ExpiresOn)
            .Select(l => l.IssuedOn)
            .FirstOrDefault(),
        LicenceExpiresOn = r.Firearm.Licences
            .OrderByDescending(l => l.ExpiresOn)
            .Select(l => (DateOnly?)l.ExpiresOn)
            .FirstOrDefault(),
        StoredFrom = r.StoredFrom,
        StoredUntil = r.StoredUntil,
        StorageStatus = r.StorageStatus,
        StorageLocation = r.StorageLocation,
        RackNumber = r.RackNumber,
        SafeNumber = r.SafeNumber,
    };
}
