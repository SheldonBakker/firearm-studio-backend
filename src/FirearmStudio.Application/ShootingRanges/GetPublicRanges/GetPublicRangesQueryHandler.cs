using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.ShootingRanges.GetPublicRanges;

public sealed class GetPublicRangesQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    : IQueryHandler<GetPublicRangesQuery, ErrorOr<IReadOnlyList<PublicRangeResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<PublicRangeResponse>>> Handle(
        GetPublicRangesQuery query, CancellationToken cancellationToken)
    {
        var companyExists = await db.Companies
            .AsNoTracking()
            .AnyAsync(c => c.Id == query.CompanyId && c.IsActive, cancellationToken);

        if (!companyExists)
        {
            return Error.NotFound(ErrorCodes.CompanyNotFound, "Company not found.");
        }

        using var scope = tenant.BeginCompanyScope(query.CompanyId);

        var ranges = await db.ShootingRanges
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Id)
            .Select(PublicRangeResponse.QueryProjection)
            .ToListAsync(cancellationToken);

        return ranges;
    }

    public static class ErrorCodes
    {
        public const string CompanyNotFound = "GetPublicRangesQuery.CompanyNotFound";
    }
}
