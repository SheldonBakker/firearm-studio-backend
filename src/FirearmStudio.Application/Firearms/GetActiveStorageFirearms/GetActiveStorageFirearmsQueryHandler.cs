using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Firearms.GetActiveStorageFirearms;

public sealed class GetActiveStorageFirearmsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetActiveStorageFirearmsQuery, ErrorOr<IReadOnlyList<ActiveStorageFirearmDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<ActiveStorageFirearmDto>>> Handle(
        GetActiveStorageFirearmsQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.StorageRecords.AsNoTracking();

        if (query.StorageStatus.HasValue)
        {
            queryable = queryable.Where(s => s.StorageStatus == query.StorageStatus.Value);
        }
        else
        {
            queryable = queryable.ActiveOpen();
        }

        if (!string.IsNullOrWhiteSpace(query.SerialNumber))
        {
            var term = query.SerialNumber.Trim().ToLower();
            queryable = queryable.Where(s => s.Firearm!.SerialNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerName))
        {
            var term = query.CustomerName.Trim().ToLower();
            queryable = queryable.Where(s =>
                (s.Firearm!.Customer!.FullName != null && s.Firearm.Customer.FullName.ToLower().Contains(term)) ||
                (s.Firearm!.Customer!.CompanyName != null && s.Firearm.Customer.CompanyName.ToLower().Contains(term)));
        }

        IReadOnlyList<ActiveStorageFirearmDto> records = await queryable
            .OrderBy(record => record.Firearm!.SerialNumber)
            .ThenBy(record => record.Id)
            .Select(ActiveStorageFirearmDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(records);
    }
}
