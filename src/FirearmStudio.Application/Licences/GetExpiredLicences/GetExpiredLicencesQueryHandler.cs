using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Licences.GetExpiredLicences;

public sealed class GetExpiredLicencesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetExpiredLicencesQuery, ErrorOr<IReadOnlyList<ExpiredLicenceDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<ExpiredLicenceDto>>> Handle(
        GetExpiredLicencesQuery query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        IReadOnlyList<ExpiredLicenceDto> licences = await db.FirearmLicences
            .AsNoTracking()
            .Where(l => l.ExpiresOn < today)
            .OrderBy(l => l.ExpiresOn)
            .Select(ExpiredLicenceDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(licences);
    }
}
