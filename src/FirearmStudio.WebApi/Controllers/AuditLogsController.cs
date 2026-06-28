using Asp.Versioning;
using FirearmStudio.Application.AuditLogs;
using FirearmStudio.Application.AuditLogs.GetAuditLogs;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/audit-logs")]
[Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
public sealed class AuditLogsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogListItemDto>>> List(
        [FromQuery] string? entityType, [FromQuery] int take, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAuditLogsQuery(entityType, take), ct);
        return result.ToActionResult();
    }
}
