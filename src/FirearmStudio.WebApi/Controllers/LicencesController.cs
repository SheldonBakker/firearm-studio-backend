using Asp.Versioning;
using FirearmStudio.Application.Licences;
using FirearmStudio.Application.Licences.CreateLicence;
using FirearmStudio.Application.Licences.GetExpiredLicences;
using FirearmStudio.Application.Licences.GetLicencesDueForRenewal;
using FirearmStudio.Application.Licences.UpdateLicence;
using FirearmStudio.Domain.Authentication;
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
    [HttpGet("licences/due-renewal")]
    public async Task<ActionResult<IReadOnlyList<LicenceDueForRenewalDto>>> DueForRenewal(CancellationToken ct)
    {
        var result = await mediator.Send(new GetLicencesDueForRenewalQuery(), ct);
        return result.ToActionResult();
    }

    [HttpGet("licences/expired")]
    public async Task<ActionResult<IReadOnlyList<ExpiredLicenceDto>>> Expired(CancellationToken ct)
    {
        var result = await mediator.Send(new GetExpiredLicencesQuery(), ct);
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
