using Asp.Versioning;
using FirearmStudio.Application.Bookings;
using FirearmStudio.Application.Bookings.CreatePublicBooking;
using FirearmStudio.Application.Bookings.GetDayAvailability;
using FirearmStudio.Application.Bookings.GetMonthAvailability;
using FirearmStudio.Application.Packages;
using FirearmStudio.Application.Packages.GetPublicPackages;
using FirearmStudio.Application.ShootingRanges;
using FirearmStudio.Application.ShootingRanges.GetPublicRanges;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/public/companies/{companyId:guid}")]
[AllowAnonymous]
[EnableRateLimiting("public")]
public sealed class PublicBookingsController(IMediator mediator) : ControllerBase
{
    [HttpGet("packages")]
    public async Task<ActionResult<IReadOnlyList<PublicPackageResponse>>> Packages(Guid companyId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPublicPackagesQuery(companyId), ct);
        return result.ToActionResult();
    }

    [HttpGet("ranges")]
    public async Task<ActionResult<IReadOnlyList<PublicRangeResponse>>> Ranges(Guid companyId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPublicRangesQuery(companyId), ct);
        return result.ToActionResult();
    }

    [HttpGet("ranges/{rangeId:guid}/availability")]
    public async Task<ActionResult<DayAvailabilityResponse>> DayAvailability(
        Guid companyId,
        Guid rangeId,
        [FromQuery] Guid packageId,
        [FromQuery] DateOnly date,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetDayAvailabilityQuery(companyId, rangeId, packageId, date), ct);
        return result.ToActionResult();
    }

    [HttpGet("ranges/{rangeId:guid}/availability/month")]
    public async Task<ActionResult<MonthAvailabilityResponse>> MonthAvailability(
        Guid companyId,
        Guid rangeId,
        [FromQuery] Guid packageId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetMonthAvailabilityQuery(companyId, rangeId, packageId, year, month), ct);
        return result.ToActionResult();
    }

    [HttpPost("bookings")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<PublicBookingConfirmationResponse>> CreateBooking(
        Guid companyId,
        CreatePublicBookingRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new CreatePublicBookingCommand(companyId, request), ct);
        return result.IsError
            ? result.ToActionResult()
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }
}
