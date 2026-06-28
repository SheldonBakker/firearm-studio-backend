using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Firearms.GetFirearmLicences;

public sealed class GetFirearmLicencesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetFirearmLicencesQuery, ErrorOr<IReadOnlyList<FirearmLicenceListItemDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<FirearmLicenceListItemDto>>> Handle(
        GetFirearmLicencesQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<FirearmLicenceListItemDto> licences = await db.FirearmLicences
            .AsNoTracking()
            .Where(l => l.FirearmId == query.FirearmId)
            .Select(FirearmLicenceListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(licences);
    }
}
