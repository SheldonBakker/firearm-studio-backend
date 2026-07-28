using Asp.Versioning;
using FirearmStudio.Application.Bookings.GetBookingIcs;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/public/bookings")]
[AllowAnonymous]
[EnableRateLimiting("public")]
public sealed class PublicCalendarController(IMediator mediator) : ControllerBase
{
    [HttpGet("{token}/calendar.ics")]
    public async Task<ActionResult> Calendar(string token, CancellationToken ct)
    {
        var result = await mediator.Send(new GetBookingIcsQuery(token), ct);
        return result.IsError
            ? result.ToActionResult()
            : File(result.Value, "text/calendar", "booking.ics");
    }
}
