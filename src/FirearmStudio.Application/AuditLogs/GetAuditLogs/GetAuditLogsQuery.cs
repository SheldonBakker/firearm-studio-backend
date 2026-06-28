using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.AuditLogs.GetAuditLogs;

public sealed record GetAuditLogsQuery(string? EntityType, int Take)
    : IQuery<ErrorOr<IReadOnlyList<AuditLogListItemDto>>>;
