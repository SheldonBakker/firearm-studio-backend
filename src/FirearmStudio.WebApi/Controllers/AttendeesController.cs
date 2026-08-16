using FirearmStudio.Application.Bookings;
using FirearmStudio.Application.Bookings.RemoveAttendee;
using FirearmStudio.Application.Bookings.UpdateAttendee;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/attendees")]
[Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
public sealed class AttendeesController(IMediator mediator) : ApiControllerBase
{
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateAttendeeRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateAttendeeCommand(id, request), ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new RemoveAttendeeCommand(id), ct);
        return result.ToActionResult();
    }
}
