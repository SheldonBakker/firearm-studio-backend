using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Firearms.GetFirearm;

public sealed class GetFirearmQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetFirearmQuery, ErrorOr<FirearmDetailResponse>>
{
    public async Task<ErrorOr<FirearmDetailResponse>> Handle(GetFirearmQuery query, CancellationToken cancellationToken)
    {
        return await db.Firearms
            .AsNoTracking()
            .Where(f => f.Id == query.Id)
            .FirstOrNotFoundAsync(FirearmDetailResponse.QueryProjection, ErrorCodes.NotFound, "Firearm not found.", cancellationToken);
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetFirearmQuery.NotFound";
    }
}
