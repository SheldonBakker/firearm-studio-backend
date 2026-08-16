using FirearmStudio.Application.Sage;
using FirearmStudio.Application.Sage.GetSageConnection;
using FirearmStudio.Application.Sage.RegisterSageConnection;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/sage")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class SageController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("connections")]
    [ProducesResponseType(typeof(SageConnectionDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SageConnectionDetailsResponse?>> GetConnection(CancellationToken ct)
    {
        var result = await mediator.Send(new GetSageConnectionQuery(), ct);
        return result.ToActionResult();
    }

    [HttpPost("register")]
    [EnableRateLimiting("sage-register")]
    public async Task<ActionResult<SageConnectionResponse>> Register(
        RegisterSageConnectionRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new RegisterSageConnectionCommand(request), ct);
        return result.ToActionResult();
    }
}
