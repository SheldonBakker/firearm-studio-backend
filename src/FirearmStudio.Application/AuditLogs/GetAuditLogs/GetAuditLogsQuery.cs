using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;

namespace FirearmStudio.Application.AuditLogs.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    int PageNumber,
    int PageSize,
    string? FullName,
    string? Action,
    string? EntityType,
    DateOnly? CreatedOn)
    : IQuery<ErrorOr<PaginatedResponse<AuditLogListItemDto>>>;
