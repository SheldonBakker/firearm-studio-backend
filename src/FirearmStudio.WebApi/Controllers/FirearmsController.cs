using FirearmStudio.Application.Firearms;
using FirearmStudio.Application.Firearms.CreateFirearm;
using FirearmStudio.Application.Firearms.GetFirearm;
using FirearmStudio.Application.Firearms.GetFirearms;
using FirearmStudio.Application.Firearms.UpdateFirearm;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Enums;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/firearms")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class FirearmsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<FirearmResponse>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? serialNumber = null,
        [FromQuery] FirearmStatus? status = null,
        [FromQuery] string? customerName = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetFirearmsQuery(pageNumber, pageSize, serialNumber, status, customerName), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FirearmDetailResponse>> Get(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetFirearmQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult<FirearmResponse>> Create(CreateFirearmRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateFirearmCommand(request), ct);
        return result.IsError
            ? result.ToActionResult()
            : CreatedAtAction(nameof(Get), new { id = result.Value.Id, version = CurrentApiVersion }, result.Value);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Update(Guid id, UpdateFirearmRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateFirearmCommand(id, request), ct);
        return result.ToActionResult();
    }
}
