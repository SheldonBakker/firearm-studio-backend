using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.AuditLogs.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetAuditLogsQuery, ErrorOr<IReadOnlyList<AuditLogListItemDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<AuditLogListItemDto>>> Handle(
        GetAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            queryable = queryable.Where(a => a.EntityType == query.EntityType);
        }

        IReadOnlyList<AuditLogListItemDto> items = await queryable
            .OrderByDescending(a => a.CreatedAt)
            .Take(query.Take <= 0 ? 100 : Math.Min(query.Take, 500))
            .Select(AuditLogListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(items);
    }
}
