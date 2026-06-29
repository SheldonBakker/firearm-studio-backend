using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.StorageRecords.GetStorageRecords;

public sealed class GetStorageRecordsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetStorageRecordsQuery, ErrorOr<IReadOnlyList<StorageRecordDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<StorageRecordDto>>> Handle(
        GetStorageRecordsQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.StorageRecords.AsNoTracking();

        if (query.StorageStatus.HasValue)
            queryable = queryable.Where(s => s.StorageStatus == query.StorageStatus.Value);

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

        IReadOnlyList<StorageRecordDto> records = await queryable
            .Select(s => new StorageRecordDto(
                s.Id,
                s.FirearmId,
                s.Firearm!.CustomerId,
                s.Firearm.Customer!.FullName ?? s.Firearm.Customer.CompanyName,
                s.Firearm.SerialNumber,
                s.StorageStatus,
                s.MonthlyRate,
                s.StorageLocation,
                s.RackNumber,
                s.SafeNumber,
                s.StoredFrom,
                s.StoredUntil))
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(records);
    }
}
