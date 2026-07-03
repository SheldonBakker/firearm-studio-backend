using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Firearms.GetFirearm;

public sealed class GetFirearmQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetFirearmQuery, ErrorOr<FirearmDetailResponse>>
{
    public async Task<ErrorOr<FirearmDetailResponse>> Handle(GetFirearmQuery query, CancellationToken cancellationToken)
    {
        var firearm = await db.Firearms
            .AsNoTracking()
            .Where(f => f.Id == query.Id)
            .Select(FirearmDetailResponse.QueryProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return firearm is null
            ? Error.NotFound(ErrorCodes.NotFound, "Firearm not found.")
            : firearm;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetFirearmQuery.NotFound";
    }
}
