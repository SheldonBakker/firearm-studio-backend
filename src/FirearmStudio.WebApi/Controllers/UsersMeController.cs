using FirearmStudio.Application.Users;
using FirearmStudio.Application.Users.UpdatePhone;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/users/me")]
[Authorize]
public sealed class UsersMeController(IMediator mediator) : ApiControllerBase
{
    [HttpPost("phone")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> UpdatePhone(UpdatePhoneRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdatePhoneCommand(request), ct);
        return result.IsError
            ? result.ToActionResult()
            : Accepted(new UpdatePhoneResponse());
    }

    [HttpPost("phone/verify")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> VerifyPhone(VerifyPhoneRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new VerifyPhoneCommand(request), ct);
        return result.IsError ? result.ToActionResult() : NoContent();
    }
}
