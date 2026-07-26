using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.StorageRecords.GetCustomerStorageRecords;

public sealed class GetCustomerStorageRecordsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetCustomerStorageRecordsQuery, ErrorOr<PaginatedResponse<CustomerStorageRecordDto>>>
{
    private const int MaxPageSize = 200;

    public async Task<ErrorOr<PaginatedResponse<CustomerStorageRecordDto>>> Handle(
        GetCustomerStorageRecordsQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

        var queryable = db.StorageRecords
            .AsNoTracking()
            .Where(s => s.Firearm!.CustomerId == query.CustomerId)
            .OrderByDescending(s => s.StoredFrom)
            .ThenBy(s => s.Id);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(CustomerStorageRecordDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<CustomerStorageRecordDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
