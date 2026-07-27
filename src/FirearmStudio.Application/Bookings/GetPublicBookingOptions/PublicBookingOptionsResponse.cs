using System.Linq.Expressions;
using FirearmStudio.Application.Packages;
using FirearmStudio.Application.ShootingRanges;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Bookings.GetPublicBookingOptions;

public sealed record PublicCompanyResponse(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Province,
    string? PostalCode,
    DepositMode DepositMode,
    decimal DepositValue,
    int DepositWindowHours)
{
    public static Expression<Func<Company, PublicCompanyResponse>> QueryProjection => c => new PublicCompanyResponse(
        c.Id, c.Name, c.Email, c.Phone, c.AddressLine1, c.AddressLine2, c.City, c.Province, c.PostalCode,
        c.DepositMode, c.DepositValue, c.DepositWindowHours);
}

public sealed record PublicBookingOptionsResponse(
    PublicCompanyResponse Company,
    IReadOnlyList<PublicPackageResponse> Packages,
    IReadOnlyList<PublicRangeResponse> Ranges);
