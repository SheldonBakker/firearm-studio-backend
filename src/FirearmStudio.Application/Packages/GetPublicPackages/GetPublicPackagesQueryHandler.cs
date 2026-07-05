using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Packages.GetPublicPackages;

public sealed class GetPublicPackagesQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    : IQueryHandler<GetPublicPackagesQuery, ErrorOr<IReadOnlyList<PublicPackageResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<PublicPackageResponse>>> Handle(
        GetPublicPackagesQuery query, CancellationToken cancellationToken)
    {
        var companyExists = await db.Companies
            .AsNoTracking()
            .AnyAsync(c => c.Id == query.CompanyId && c.IsActive, cancellationToken);

        if (!companyExists)
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

        return packages;
    }

    public static class ErrorCodes
    {
        public const string CompanyNotFound = "GetPublicPackagesQuery.CompanyNotFound";
    }
}
