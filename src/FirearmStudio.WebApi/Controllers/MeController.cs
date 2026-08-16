using FirearmStudio.Application.Me;
using FirearmStudio.Application.Me.GetAdminCheck;
using FirearmStudio.Application.Me.GetCurrentUser;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/me")]
[Authorize]
public sealed class MeController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CurrentUserResponse>> Get(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCurrentUserQuery(), ct);
        return result.ToActionResult();
    }

    [HttpGet("admin-check")]
    [Authorize(Roles = AppRoles.Policy.AdminOnly)]
    public async Task<ActionResult<AdminCheckResponse>> AdminCheck(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAdminCheckQuery(), ct);
        return result.ToActionResult();
    }
}
