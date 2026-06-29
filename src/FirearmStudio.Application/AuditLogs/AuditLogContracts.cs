using System.Linq.Expressions;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.AuditLogs;

public sealed record AuditLogUserDto(
    Guid Id,
    string? FullName,
    string Email,
    AppRole Role);

public sealed record AuditLogListItemDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? OldValue,
    string? NewValue,
    DateTime CreatedAt,
    AuditLogUserDto? User)
{
    public static Expression<Func<AuditLog, AuditLogListItemDto>> QueryProjection => a => new AuditLogListItemDto(
        a.Id,
        a.EntityType,
        a.EntityId,
        a.Action,
        a.OldValue,
        a.NewValue,
        a.CreatedAt,
        a.AppUser == null
            ? null
            : new AuditLogUserDto(a.AppUser.Id, a.AppUser.FullName, a.AppUser.Email, a.AppUser.Role));
}
