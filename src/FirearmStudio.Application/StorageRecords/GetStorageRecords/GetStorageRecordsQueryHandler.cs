using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
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
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.SerialNumber.Trim());
            queryable = queryable.Where(s => EF.Functions.ILike(s.Firearm!.SerialNumber, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerName))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.CustomerName.Trim());
            queryable = queryable.Where(s =>
                (s.Firearm!.Customer!.FullName != null && EF.Functions.ILike(s.Firearm.Customer.FullName, pattern)) ||
                (s.Firearm!.Customer!.CompanyName != null && EF.Functions.ILike(s.Firearm.Customer.CompanyName, pattern)));
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
