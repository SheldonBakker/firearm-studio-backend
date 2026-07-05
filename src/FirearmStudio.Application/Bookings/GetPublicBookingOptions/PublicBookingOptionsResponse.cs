using System.Linq.Expressions;
using FirearmStudio.Application.Packages;
using FirearmStudio.Application.ShootingRanges;
using FirearmStudio.Domain.Entities;

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
    string? PostalCode)
{
    public static Expression<Func<Company, PublicCompanyResponse>> QueryProjection => c => new PublicCompanyResponse(
        c.Id, c.Name, c.Email, c.Phone, c.AddressLine1, c.AddressLine2, c.City, c.Province, c.PostalCode);
}

public sealed record PublicBookingOptionsResponse(
    PublicCompanyResponse Company,
    IReadOnlyList<PublicPackageResponse> Packages,
    IReadOnlyList<PublicRangeResponse> Ranges);
