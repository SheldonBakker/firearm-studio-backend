using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Packages.GetPackage;

public sealed class GetPackageQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetPackageQuery, ErrorOr<PackageResponse>>
{
    public async Task<ErrorOr<PackageResponse>> Handle(GetPackageQuery query, CancellationToken cancellationToken)
    {
        return await db.Packages
            .AsNoTracking()
            .Where(p => p.Id == query.Id)
            .FirstOrNotFoundAsync(PackageResponse.QueryProjection, ErrorCodes.NotFound, "Package not found.", cancellationToken);
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetPackageQuery.NotFound";
    }
}
