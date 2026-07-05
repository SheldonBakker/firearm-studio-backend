using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Packages.GetPackage;

public sealed class GetPackageQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetPackageQuery, ErrorOr<PackageResponse>>
{
    public async Task<ErrorOr<PackageResponse>> Handle(GetPackageQuery query, CancellationToken cancellationToken)
    {
        var package = await db.Packages
            .AsNoTracking()
            .Where(p => p.Id == query.Id)
            .Select(PackageResponse.QueryProjection)
            .FirstOrDefaultAsync(cancellationToken);

        if (package is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Package not found.");
        }

        return package;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetPackageQuery.NotFound";
    }
}
