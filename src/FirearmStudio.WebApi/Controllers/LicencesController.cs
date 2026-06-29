using Asp.Versioning;
using FirearmStudio.Application.Licences;
using FirearmStudio.Application.Licences.CreateLicence;
using FirearmStudio.Application.Licences.GetLicences;
using FirearmStudio.Application.Licences.UpdateLicence;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Enums;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class LicencesController(IMediator mediator) : ControllerBase
{
    [HttpGet("licences")]
    public async Task<ActionResult<PaginatedResponse<LicenceListItemDto>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? licenceNumber = null,
        [FromQuery] LicenceStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetLicencesQuery(pageNumber, pageSize, sortOrder, licenceNumber, status), ct);
        return result.ToActionResult();
    }

    [HttpPost("firearms/{firearmId:guid}/licences")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Create(Guid firearmId, CreateLicenceRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateLicenceCommand(firearmId, request), ct);
        return result.IsError
            ? result.ToActionResult()
            : Created($"/api/v1/firearms/{firearmId}/licences", new { Id = result.Value });
    }

    [HttpPatch("licences/{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Update(Guid id, UpdateLicenceRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateLicenceCommand(id, request), ct);
        return result.ToActionResult();
    }
}
