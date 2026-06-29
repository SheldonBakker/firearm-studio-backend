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
            .OrderByDescending(s => s.StoredFrom)
            .ThenBy(s => s.Id)
            .Select(CustomerStorageRecordDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(records);
    }
}
