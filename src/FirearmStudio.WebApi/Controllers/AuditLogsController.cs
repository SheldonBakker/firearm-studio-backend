using FirearmStudio.Application.AuditLogs;
using FirearmStudio.Application.AuditLogs.GetAuditLogs;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/audit-logs")]
[Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
public sealed class AuditLogsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AuditLogListItemDto>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? fullName = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] DateOnly? createdOn = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetAuditLogsQuery(pageNumber, pageSize, fullName, action, entityType, createdOn), ct);
        return result.ToActionResult();
    }
}
