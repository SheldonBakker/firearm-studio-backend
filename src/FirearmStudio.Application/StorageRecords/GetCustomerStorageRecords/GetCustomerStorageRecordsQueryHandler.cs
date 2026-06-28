using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.StorageRecords.GetCustomerStorageRecords;

public sealed class GetCustomerStorageRecordsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetCustomerStorageRecordsQuery, ErrorOr<IReadOnlyList<CustomerStorageRecordDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<CustomerStorageRecordDto>>> Handle(
        GetCustomerStorageRecordsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomerStorageRecordDto> records = await db.StorageRecords
            .AsNoTracking()
            .Where(s => s.Firearm!.CustomerId == query.CustomerId)
            .Select(s => new CustomerStorageRecordDto(
                s.Id, s.FirearmId, s.MonthlyRate, s.StorageStatus, s.StoredFrom, s.StoredUntil))
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(records);
    }
}
