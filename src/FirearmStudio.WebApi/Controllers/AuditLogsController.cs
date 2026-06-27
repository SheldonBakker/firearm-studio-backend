using Asp.Versioning;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/audit-logs")]
[Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
public sealed class AuditLogsController(IApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] string? entityType, [FromQuery] int take, CancellationToken ct)
    {
        var query = db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        return Ok(await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(take <= 0 ? 100 : Math.Min(take, 500))
            .Select(a => new { a.Id, a.AppUserId, a.EntityType, a.EntityId, a.Action, a.CreatedAt })
            .ToListAsync(ct));
    }
}
