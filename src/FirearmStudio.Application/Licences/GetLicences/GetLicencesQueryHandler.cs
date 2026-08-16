using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Licences.GetLicences;

public sealed class GetLicencesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetLicencesQuery, ErrorOr<PaginatedResponse<LicenceListItemDto>>>
{
    public async Task<ErrorOr<PaginatedResponse<LicenceListItemDto>>> Handle(
        GetLicencesQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.FirearmLicences.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.LicenceNumber))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.LicenceNumber.Trim());
            queryable = queryable.Where(l => EF.Functions.ILike(l.LicenceNumber, pattern));
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(l => l.Status == query.Status.Value);
        }

        queryable = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? queryable.OrderByDescending(l => l.ExpiresOn).ThenBy(l => l.Id)
            : queryable.OrderBy(l => l.ExpiresOn).ThenBy(l => l.Id);

        return await queryable.ToPaginatedAsync(
            query.PageNumber, query.PageSize, LicenceListItemDto.QueryProjection, cancellationToken);
    }
}
