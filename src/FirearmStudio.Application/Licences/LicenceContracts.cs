using System.Linq.Expressions;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Licences;

public sealed record LicenceListItemDto(
    Guid Id, Guid FirearmId, string LicenceNumber,
    DateOnly? IssuedOn, DateOnly ExpiresOn, DateOnly RenewalDueOn, LicenceStatus Status)
{
    public static Expression<Func<FirearmLicence, LicenceListItemDto>> QueryProjection => l => new LicenceListItemDto(
        l.Id, l.FirearmId, l.LicenceNumber, l.IssuedOn, l.ExpiresOn, l.RenewalDueOn, l.Status);
}

public sealed record LicenceDetailDto(
    Guid Id, Guid FirearmId, string LicenceNumber,
    DateOnly? IssuedOn, DateOnly ExpiresOn, DateOnly RenewalDueOn,
    LicenceStatus Status, string? DocumentUrl,
    LicenceFirearmDto Firearm, LicenceCustomerDto Customer)
{
    public static Expression<Func<FirearmLicence, LicenceDetailDto>> QueryProjection => l => new LicenceDetailDto(
        l.Id, l.FirearmId, l.LicenceNumber, l.IssuedOn, l.ExpiresOn, l.RenewalDueOn,
        l.Status, l.DocumentUrl,
        new LicenceFirearmDto(
            l.Firearm!.Id, l.Firearm.Make, l.Firearm.Model, l.Firearm.Calibre,
            l.Firearm.FirearmType, l.Firearm.SerialNumber, l.Firearm.Status),
        new LicenceCustomerDto(
            l.Firearm.Customer!.Id, l.Firearm.Customer.CustomerType, l.Firearm.Customer.FullName,
            l.Firearm.Customer.CompanyName, l.Firearm.Customer.Email, l.Firearm.Customer.Phone));
}

public sealed record LicenceFirearmDto(
    Guid Id, string Make, string? Model, string? Calibre,
    string? FirearmType, string SerialNumber, FirearmStatus Status);

public sealed record LicenceCustomerDto(
    Guid Id, CustomerType CustomerType, string? FullName,
    string? CompanyName, string? Email, string? Phone);

public sealed record CreateLicenceRequest(string LicenceNumber, DateOnly? IssuedOn, DateOnly ExpiresOn, string? DocumentUrl);

public sealed record CreateLicenceResponse(Guid Id);

public sealed record UpdateLicenceRequest(
    Optional<string> LicenceNumber,
    Optional<DateOnly?> IssuedOn,
    Optional<DateOnly> ExpiresOn,
    Optional<LicenceStatus> Status,
    Optional<string?> DocumentUrl);
