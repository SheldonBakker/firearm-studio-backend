using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Firearms.GetFirearm;

public sealed class GetFirearmQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetFirearmQuery, ErrorOr<FirearmResponse>>
{
    public async Task<ErrorOr<FirearmResponse>> Handle(GetFirearmQuery query, CancellationToken cancellationToken)
    {
        var firearm = await db.Firearms
            .AsNoTracking()
            .Where(f => f.Id == query.Id)
            .Select(FirearmResponse.QueryProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return firearm is null
            ? Error.NotFound("GetFirearmQuery.NotFound", "Firearm not found.")
            : firearm;
    }
}
