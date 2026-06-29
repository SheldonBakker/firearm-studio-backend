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
        var queryable = db.Firearms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SerialNumber))
        {
            var term = query.SerialNumber.Trim().ToLower();
            queryable = queryable.Where(f => f.SerialNumber.ToLower().Contains(term));
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(f => f.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerName))
        {
            var term = query.CustomerName.Trim().ToLower();
            queryable = queryable.Where(f =>
                (f.Customer!.FullName != null && f.Customer.FullName.ToLower().Contains(term)) ||
                (f.Customer!.CompanyName != null && f.Customer.CompanyName.ToLower().Contains(term)));
        }

        IReadOnlyList<FirearmResponse> firearms = await queryable
            .OrderBy(f => f.SerialNumber)
            .ThenBy(f => f.Id)
            .Select(FirearmResponse.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(firearms);
    }
}
