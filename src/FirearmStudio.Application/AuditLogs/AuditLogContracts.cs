using System.Linq.Expressions;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.AuditLogs;

public sealed record AuditLogListItemDto(
    Guid Id,
    Guid? AppUserId,
    string EntityType,
    Guid EntityId,
    string Action,
    DateTime CreatedAt)
{
    public static Expression<Func<AuditLog, AuditLogListItemDto>> QueryProjection => a => new AuditLogListItemDto(
        a.Id, a.AppUserId, a.EntityType, a.EntityId, a.Action, a.CreatedAt);
}
