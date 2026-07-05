using Asp.Versioning;
using FirearmStudio.Application.Model;
using FirearmStudio.Application.Packages;
using FirearmStudio.Application.Packages.CreatePackage;
using FirearmStudio.Application.Packages.DeletePackage;
using FirearmStudio.Application.Packages.GetPackage;
using FirearmStudio.Application.Packages.GetPackages;
using FirearmStudio.Application.Packages.UpdatePackage;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/packages")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class PackagesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<PackageListItemDto>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? name = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetPackagesQuery(pageNumber, pageSize, sortBy, sortOrder, name, isActive), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PackageResponse>> Get(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPackageQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult<PackageResponse>> Create(CreatePackageRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreatePackageCommand(request), ct);
        return result.IsError
            ? result.ToActionResult()
            : CreatedAtAction(nameof(Get), new { id = result.Value.Id, version = "1" }, result.Value);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Update(Guid id, UpdatePackageRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdatePackageCommand(id, request), ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeletePackageCommand(id), ct);
        return result.ToActionResult();
    }
}
