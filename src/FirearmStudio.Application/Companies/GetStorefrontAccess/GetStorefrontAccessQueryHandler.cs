using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Companies.GetStorefrontAccess;

public sealed class GetStorefrontAccessQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetStorefrontAccessQuery, ErrorOr<StorefrontKeyResult>>
{
    public async Task<ErrorOr<StorefrontKeyResult>> Handle(GetStorefrontAccessQuery query, CancellationToken cancellationToken)
    {
        if (currentUserService.User.CompanyId is not { } companyId)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Company not found.");
        }

        var result = await db.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => new { c.StorefrontKey })
            .FirstOrDefaultAsync(cancellationToken);

        return result is null
            ? Error.NotFound(ErrorCodes.NotFound, "Company not found.")
            : new StorefrontKeyResult(companyId, result.StorefrontKey);
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetStorefrontAccessQuery.NotFound";
    }
}
