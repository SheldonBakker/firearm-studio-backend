using System.Linq.Expressions;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Firearms;

public sealed record FirearmResponse(
    Guid Id,
    Guid CustomerId,
    string Make,
    string? Model,
    string? Calibre,
    string? FirearmType,
    string SerialNumber,
    FirearmStatus Status,
    string? Notes)
{
    public static Expression<Func<Firearm, FirearmResponse>> QueryProjection => f => new FirearmResponse(
        f.Id, f.CustomerId, f.Make, f.Model, f.Calibre, f.FirearmType, f.SerialNumber, f.Status, f.Notes);

    public static FirearmResponse FromEntity(Firearm f) =>
        new(f.Id, f.CustomerId, f.Make, f.Model, f.Calibre, f.FirearmType, f.SerialNumber, f.Status, f.Notes);
}

public sealed record ActiveStorageFirearmDto(
    Guid FirearmId,
    Guid CustomerId,
    string? CustomerName,
    string SerialNumber,
    string Make,
    string? Model,
    decimal MonthlyRate,
    string? StorageLocation,
    DateOnly StoredFrom);

public sealed record FirearmLicenceListItemDto(
    Guid Id,
    string LicenceNumber,
    DateOnly? IssuedOn,
    DateOnly ExpiresOn,
    DateOnly RenewalDueOn,
    LicenceStatus Status)
{
    public static Expression<Func<FirearmLicence, FirearmLicenceListItemDto>> QueryProjection => l => new FirearmLicenceListItemDto(
        l.Id, l.LicenceNumber, l.IssuedOn, l.ExpiresOn, l.RenewalDueOn, l.Status);
}

public sealed record CreateFirearmRequest(
    Guid CustomerId, string Make, string? Model, string? Calibre, string? FirearmType,
    string SerialNumber, string? InternalReference, string? Notes);

public sealed record UpdateFirearmRequest(
    Optional<string?> Model,
    Optional<string?> Calibre,
    Optional<string?> FirearmType,
    Optional<string?> Notes,
    Optional<FirearmStatus> Status);
