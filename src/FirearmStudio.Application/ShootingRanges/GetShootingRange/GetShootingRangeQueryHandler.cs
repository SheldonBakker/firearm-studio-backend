using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.ShootingRanges.GetShootingRange;

public sealed class GetShootingRangeQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetShootingRangeQuery, ErrorOr<ShootingRangeResponse>>
{
    public async Task<ErrorOr<ShootingRangeResponse>> Handle(
        GetShootingRangeQuery query, CancellationToken cancellationToken)
    {
        return await db.ShootingRanges
            .AsNoTracking()
            .Where(r => r.Id == query.Id)
            .FirstOrNotFoundAsync(ShootingRangeResponse.QueryProjection, ErrorCodes.NotFound, "Shooting range not found.", cancellationToken);
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetShootingRangeQuery.NotFound";
    }
}
