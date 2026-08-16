using FirearmStudio.Application.Dashboard;
using FirearmStudio.Application.Dashboard.GetDashboardStats;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/dashboard")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class DashboardController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsResponse>> Stats(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDashboardStatsQuery(), ct);
        return result.ToActionResult();
    }
}
