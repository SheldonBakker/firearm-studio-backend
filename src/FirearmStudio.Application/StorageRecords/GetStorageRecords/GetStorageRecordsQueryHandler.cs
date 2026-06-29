using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.StorageRecords.GetStorageRecords;

public sealed class GetStorageRecordsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetStorageRecordsQuery, ErrorOr<PaginatedResponse<StorageRecordDto>>>
{
    private const int MaxPageSize = 200;

    public async Task<ErrorOr<PaginatedResponse<StorageRecordDto>>> Handle(
        GetStorageRecordsQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

        var queryable = db.StorageRecords.AsNoTracking();

        if (query.StorageStatus.HasValue)
        {
            queryable = queryable.Where(s => s.StorageStatus == query.StorageStatus.Value);
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

        queryable = queryable
            .OrderByDescending(record => record.StoredFrom)
            .ThenBy(record => record.Id);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(StorageRecordDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<StorageRecordDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
