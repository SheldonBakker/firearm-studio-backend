using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Firearms.GetFirearms;

public sealed class GetFirearmsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetFirearmsQuery, ErrorOr<IReadOnlyList<FirearmResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<FirearmResponse>>> Handle(GetFirearmsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<FirearmResponse> firearms = await db.Firearms
            .AsNoTracking()
            .OrderBy(f => f.SerialNumber)
            .Select(FirearmResponse.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(firearms);
    }
}
