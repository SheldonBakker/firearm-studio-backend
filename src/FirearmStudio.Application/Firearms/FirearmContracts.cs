using System.Linq.Expressions;
using FirearmStudio.Application.Customers;
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

public sealed record FirearmDetailResponse(
    Guid Id,
    CustomerResponse Customer,
    string Make,
    string? Model,
    string? Calibre,
    string? FirearmType,
    string SerialNumber,
    FirearmStatus Status,
    string? Notes,
    IReadOnlyList<FirearmLicenceListItemDto> Licences)
{
    public static Expression<Func<Firearm, FirearmDetailResponse>> QueryProjection => f => new FirearmDetailResponse(
        f.Id,
        new CustomerResponse(
            f.Customer!.Id, f.Customer.CustomerType, f.Customer.FullName, f.Customer.CompanyName,
            f.Customer.Email, f.Customer.Phone, f.Customer.Notes, f.Customer.IsActive),
        f.Make, f.Model, f.Calibre, f.FirearmType, f.SerialNumber, f.Status, f.Notes,
        f.Licences
            .Select(l => new FirearmLicenceListItemDto(
                l.Id, l.LicenceNumber, l.IssuedOn, l.ExpiresOn, l.RenewalDueOn, l.Status))
            .ToList());
}

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
