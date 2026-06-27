using Asp.Versioning;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using FirearmStudio.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/firearms")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class FirearmsController(IApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FirearmResponse>>> List(CancellationToken ct) =>
        await db.Firearms.OrderBy(f => f.SerialNumber)
            .Select(f => new FirearmResponse(f.Id, f.CustomerId, f.Make, f.Model, f.Calibre, f.FirearmType, f.SerialNumber, f.Status))
            .ToListAsync(ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FirearmResponse>> Get(Guid id, CancellationToken ct)
    {
        var f = await db.Firearms.FirstOrDefaultAsync(x => x.Id == id, ct);
        return f is null ? NotFound() : ToResponse(f);
    }

    [HttpGet("storage/active")]
    public async Task<ActionResult> ActiveStorage(CancellationToken ct) =>
        Ok(await db.StorageRecords
            .ActiveOpen()
            .Select(s => new
            {
                s.FirearmId,
                s.Firearm!.SerialNumber,
                s.Firearm.Make,
                s.Firearm.Model,
                s.MonthlyRate,
                s.StorageLocation,
                s.StoredFrom,
            })
            .ToListAsync(ct));

    [HttpGet("{id:guid}/licences")]
    public async Task<ActionResult> Licences(Guid id, CancellationToken ct) =>
        Ok(await db.FirearmLicences.Where(l => l.FirearmId == id)
            .Select(l => new { l.Id, l.LicenceNumber, l.IssuedOn, l.ExpiresOn, l.RenewalDueOn, l.Status })
            .ToListAsync(ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult<FirearmResponse>> Create(CreateFirearmRequest request, CancellationToken ct)
    {
        var customerExists = await db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists)
        {
            return Problem(detail: "Customer not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var firearm = new Firearm
        {
            CustomerId = request.CustomerId,
            Make = request.Make,
            Model = request.Model,
            Calibre = request.Calibre,
            FirearmType = request.FirearmType,
            SerialNumber = request.SerialNumber,
            InternalReference = request.InternalReference,
            Notes = request.Notes,
        };
        await db.Firearms.AddAsync(firearm, ct);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = firearm.Id, version = "1" }, ToResponse(firearm));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Update(Guid id, UpdateFirearmRequest request, CancellationToken ct)
    {
        var firearm = await db.Firearms.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (firearm is null)
        {
            return NotFound();
        }

        firearm.Model = request.Model ?? firearm.Model;
        firearm.Calibre = request.Calibre ?? firearm.Calibre;
        firearm.FirearmType = request.FirearmType ?? firearm.FirearmType;
        firearm.Notes = request.Notes ?? firearm.Notes;
        if (request.Status is { } status)
        {
            firearm.Status = status;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static FirearmResponse ToResponse(Firearm f) =>
        new(f.Id, f.CustomerId, f.Make, f.Model, f.Calibre, f.FirearmType, f.SerialNumber, f.Status);

    public sealed record FirearmResponse(
        Guid Id, Guid CustomerId, string Make, string? Model, string? Calibre, string? FirearmType, string SerialNumber, FirearmStatus Status);

    public sealed record CreateFirearmRequest(
        Guid CustomerId, string Make, string? Model, string? Calibre, string? FirearmType,
        string SerialNumber, string? InternalReference, string? Notes);

    public sealed record UpdateFirearmRequest(
        string? Model, string? Calibre, string? FirearmType, string? Notes, FirearmStatus? Status);
}
