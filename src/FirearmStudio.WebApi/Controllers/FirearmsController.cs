using Asp.Versioning;
using FirearmStudio.Application.Firearms;
using FirearmStudio.Application.Firearms.CreateFirearm;
using FirearmStudio.Application.Firearms.GetActiveStorageFirearms;
using FirearmStudio.Application.Firearms.GetFirearm;
using FirearmStudio.Application.Firearms.GetFirearmLicences;
using FirearmStudio.Application.Firearms.GetFirearms;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Application.Firearms.UpdateFirearm;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/firearms")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class FirearmsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FirearmResponse>>> List(
        [FromQuery] string? serialNumber,
        [FromQuery] FirearmStatus? status,
        [FromQuery] string? customerName,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetFirearmsQuery(serialNumber, status, customerName), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FirearmResponse>> Get(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetFirearmQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpGet("storage/active")]
    public async Task<ActionResult<IReadOnlyList<ActiveStorageFirearmDto>>> ActiveStorage(
        [FromQuery] string? serialNumber,
        [FromQuery] string? customerName,
        [FromQuery] StorageStatus? storageStatus,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetActiveStorageFirearmsQuery(serialNumber, customerName, storageStatus), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/licences")]
    public async Task<ActionResult<IReadOnlyList<FirearmLicenceListItemDto>>> Licences(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetFirearmLicencesQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult<FirearmResponse>> Create(CreateFirearmRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateFirearmCommand(request), ct);
        return result.IsError
            ? result.ToActionResult()
            : CreatedAtAction(nameof(Get), new { id = result.Value.Id, version = "1" }, result.Value);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Update(Guid id, UpdateFirearmRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateFirearmCommand(id, request), ct);
        return result.ToActionResult();
    }
}
