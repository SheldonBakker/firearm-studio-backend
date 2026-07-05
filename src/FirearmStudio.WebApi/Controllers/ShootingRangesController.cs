using Asp.Versioning;
using FirearmStudio.Application.Bookings;
using FirearmStudio.Application.Bookings.GetDayAvailability;
using FirearmStudio.Application.Bookings.GetMonthAvailability;
using FirearmStudio.Application.Model;
using FirearmStudio.Application.ShootingRanges;
using FirearmStudio.Application.ShootingRanges.CreateShootingRange;
using FirearmStudio.Application.ShootingRanges.GetShootingRange;
using FirearmStudio.Application.ShootingRanges.GetShootingRanges;
using FirearmStudio.Application.ShootingRanges.UpdateShootingRange;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/ranges")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class ShootingRangesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ShootingRangeListItemDto>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? name = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetShootingRangesQuery(pageNumber, pageSize, sortOrder, name, isActive), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShootingRangeResponse>> Get(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetShootingRangeQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult<ShootingRangeResponse>> Create(CreateShootingRangeRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateShootingRangeCommand(request), ct);
        return result.IsError
            ? result.ToActionResult()
            : CreatedAtAction(nameof(Get), new { id = result.Value.Id, version = "1" }, result.Value);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Update(Guid id, UpdateShootingRangeRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateShootingRangeCommand(id, request), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/availability")]
    public async Task<ActionResult<DayAvailabilityResponse>> DayAvailability(
        Guid id,
        [FromQuery] Guid packageId,
        [FromQuery] DateOnly date,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetDayAvailabilityQuery(null, id, packageId, date), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/availability/month")]
    public async Task<ActionResult<MonthAvailabilityResponse>> MonthAvailability(
        Guid id,
        [FromQuery] Guid packageId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetMonthAvailabilityQuery(null, id, packageId, year, month), ct);
        return result.ToActionResult();
    }
}
