using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.ShootingRanges.GetShootingRange;

public sealed class GetShootingRangeQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetShootingRangeQuery, ErrorOr<ShootingRangeResponse>>
{
    public async Task<ErrorOr<ShootingRangeResponse>> Handle(
        GetShootingRangeQuery query, CancellationToken cancellationToken)
    {
        var range = await db.ShootingRanges
            .AsNoTracking()
            .Where(r => r.Id == query.Id)
            .Select(ShootingRangeResponse.QueryProjection)
            .FirstOrDefaultAsync(cancellationToken);

        if (range is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Shooting range not found.");
        }

        return range;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetShootingRangeQuery.NotFound";
    }
}
