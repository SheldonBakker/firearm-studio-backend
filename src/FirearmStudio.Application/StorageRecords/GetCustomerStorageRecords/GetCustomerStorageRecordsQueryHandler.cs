using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.StorageRecords.GetCustomerStorageRecords;

public sealed class GetCustomerStorageRecordsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetCustomerStorageRecordsQuery, ErrorOr<PaginatedResponse<CustomerStorageRecordDto>>>
{
    public async Task<ErrorOr<PaginatedResponse<CustomerStorageRecordDto>>> Handle(
        GetCustomerStorageRecordsQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.StorageRecords
            .AsNoTracking()
            .Where(s => s.Firearm!.CustomerId == query.CustomerId)
            .OrderByDescending(s => s.StoredFrom)
            .ThenBy(s => s.Id);

        return await queryable.ToPaginatedAsync(
            query.PageNumber, query.PageSize, CustomerStorageRecordDto.QueryProjection, cancellationToken);
    }
}
