using FirearmStudio.Application.Bookings;
using FirearmStudio.Application.Bookings.AddAttendee;
using FirearmStudio.Application.Bookings.CancelBooking;
using FirearmStudio.Application.Bookings.CheckInBooking;
using FirearmStudio.Application.Bookings.CompleteBooking;
using FirearmStudio.Application.Bookings.ConfirmBooking;
using FirearmStudio.Application.Bookings.CreateBooking;
using FirearmStudio.Application.Bookings.ExportRegister;
using FirearmStudio.Application.Bookings.GetBooking;
using FirearmStudio.Application.Bookings.GetBookingAttendees;
using FirearmStudio.Application.Bookings.GetBookingCalendar;
using FirearmStudio.Application.Bookings.GetBookings;
using FirearmStudio.Application.Bookings.GetRegister;
using FirearmStudio.Application.Bookings.MarkBookingNoShow;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Enums;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/bookings")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class BookingsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<BookingListItemDto>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortOrder = "desc",
        [FromQuery] Guid? rangeId = null,
        [FromQuery] BookingStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetBookingsQuery(pageNumber, pageSize, sortOrder, rangeId, status, customerId, dateFrom, dateTo), ct);
        return result.ToActionResult();
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<IReadOnlyList<BookingCalendarItemDto>>> Calendar(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? rangeId = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetBookingCalendarQuery(year, month, rangeId), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> Get(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetBookingQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult<BookingResponse>> Create(CreateBookingRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateBookingCommand(request), ct);
        return result.IsError
            ? result.ToActionResult()
            : CreatedAtAction(nameof(Get), new { id = result.Value.Id, version = CurrentApiVersion }, result.Value);
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ConfirmBookingCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelBookingCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new CompleteBookingCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/no-show")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> NoShow(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new MarkBookingNoShowCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/check-in")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> CheckIn(Guid id, CheckInBookingRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CheckInBookingCommand(id, request), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/attendees")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> AddAttendee(Guid id, AttendeeRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new AddAttendeeCommand(id, request), ct);
        return result.IsError
            ? result.ToActionResult()
            : Created(VersionedUrl($"bookings/{id}/attendees"), new CreateAttendeeResponse(result.Value));
    }

    [HttpGet("{id:guid}/attendees")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult<IReadOnlyList<AttendeeResponse>>> GetAttendees(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetBookingAttendeesQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpGet("register")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult<PaginatedResponse<RegisterRowDto>>> GetRegister(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] Guid? rangeId = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetRegisterQuery(pageNumber, pageSize, dateFrom, dateTo, rangeId), ct);
        return result.ToActionResult();
    }

    [HttpGet("register/export")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> ExportRegister(
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] Guid? rangeId = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new ExportRegisterQuery(dateFrom, dateTo, rangeId), ct);
        if (result.IsError)
        {
            return result.ToActionResult();
        }

        var from = dateFrom?.ToString("yyyy-MM-dd") ?? "all";
        var to = dateTo?.ToString("yyyy-MM-dd") ?? "all";
        return File(result.Value, "text/csv; charset=utf-8", $"range-register-{from}-{to}.csv");
    }
}
