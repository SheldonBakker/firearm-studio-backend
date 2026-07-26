using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.AuditLogs.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetAuditLogsQuery, ErrorOr<PaginatedResponse<AuditLogListItemDto>>>
{
    private const int MaxPageSize = 200;

    public async Task<ErrorOr<PaginatedResponse<AuditLogListItemDto>>> Handle(
        GetAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

        var queryable = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.FullName))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.FullName.Trim());
            queryable = queryable.Where(a =>
                a.AppUser != null &&
                a.AppUser.FullName != null &&
                EF.Functions.ILike(a.AppUser.FullName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var pattern = SearchPatternHelper.ToILikeExactPattern(query.Action.Trim());
            queryable = queryable.Where(a => EF.Functions.ILike(a.Action, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            queryable = queryable.Where(a => a.EntityType == query.EntityType);
        }

        if (query.CreatedOn is { } createdOn)
        {
            var start = createdOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = start.AddDays(1);
            queryable = queryable.Where(a => a.CreatedAt >= start && a.CreatedAt < end);
        }

        queryable = queryable
            .OrderByDescending(a => a.CreatedAt)
            .ThenBy(a => a.Id);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(AuditLogListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<AuditLogListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
