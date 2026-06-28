using System.Linq.Expressions;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Licences;

public sealed record LicenceDueForRenewalDto(
    Guid Id, Guid FirearmId, string LicenceNumber, DateOnly ExpiresOn, DateOnly RenewalDueOn, LicenceStatus Status)
{
    public static Expression<Func<FirearmLicence, LicenceDueForRenewalDto>> QueryProjection => l => new LicenceDueForRenewalDto(
        l.Id, l.FirearmId, l.LicenceNumber, l.ExpiresOn, l.RenewalDueOn, l.Status);
}

public sealed record ExpiredLicenceDto(
    Guid Id, Guid FirearmId, string LicenceNumber, DateOnly ExpiresOn, LicenceStatus Status)
{
    public static Expression<Func<FirearmLicence, ExpiredLicenceDto>> QueryProjection => l => new ExpiredLicenceDto(
        l.Id, l.FirearmId, l.LicenceNumber, l.ExpiresOn, l.Status);
}

public sealed record CreateLicenceRequest(string LicenceNumber, DateOnly? IssuedOn, DateOnly ExpiresOn, string? DocumentUrl);

public sealed record UpdateLicenceRequest(
    Optional<string> LicenceNumber,
    Optional<DateOnly?> IssuedOn,
    Optional<DateOnly> ExpiresOn,
    Optional<LicenceStatus> Status,
    Optional<string?> DocumentUrl);
