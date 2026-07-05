using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Packages;
using FirearmStudio.Application.ShootingRanges;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.GetPublicBookingOptions;

public sealed class GetPublicBookingOptionsQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    : IQueryHandler<GetPublicBookingOptionsQuery, ErrorOr<PublicBookingOptionsResponse>>
{
    public async Task<ErrorOr<PublicBookingOptionsResponse>> Handle(
        GetPublicBookingOptionsQuery query, CancellationToken cancellationToken)
    {
        var company = await db.Companies
            .AsNoTracking()
            .Where(c => c.Id == query.CompanyId && c.IsActive)
            .Select(PublicCompanyResponse.QueryProjection)
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
        {
            return Error.NotFound(ErrorCodes.CompanyNotFound, "Company not found.");
        }

        using var scope = tenant.BeginCompanyScope(query.CompanyId);

        var packages = await db.Packages
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Select(PublicPackageResponse.QueryProjection)
            .ToListAsync(cancellationToken);

        var ranges = await db.ShootingRanges
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Id)
            .Select(PublicRangeResponse.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PublicBookingOptionsResponse(company, packages, ranges);
    }

    public static class ErrorCodes
    {
        public const string CompanyNotFound = "GetPublicBookingOptionsQuery.CompanyNotFound";
    }
}
