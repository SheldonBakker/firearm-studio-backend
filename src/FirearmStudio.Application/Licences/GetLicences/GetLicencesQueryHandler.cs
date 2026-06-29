using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Licences.GetLicences;

public sealed class GetLicencesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetLicencesQuery, ErrorOr<IReadOnlyList<LicenceListItemDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<LicenceListItemDto>>> Handle(
        GetLicencesQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.FirearmLicences.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.LicenceNumber))
        {
            var term = query.LicenceNumber.Trim().ToLower();
            queryable = queryable.Where(l => l.LicenceNumber.ToLower().Contains(term));
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(l => l.Status == query.Status.Value);
        }

        queryable = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? queryable.OrderByDescending(l => l.ExpiresOn).ThenBy(l => l.Id)
            : queryable.OrderBy(l => l.ExpiresOn).ThenBy(l => l.Id);

        IReadOnlyList<LicenceListItemDto> licences = await queryable
            .Select(LicenceListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(licences);
    }
}
