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
[Route("api/v{version:apiVersion}")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class StorageRecordsController(IApplicationDbContext db) : ControllerBase
{
    [HttpGet("storage/active")]
    public async Task<ActionResult> Active(CancellationToken ct) =>
        Ok(await db.StorageRecords
            .ActiveOpen()
            .Select(s => new { s.Id, s.FirearmId, s.MonthlyRate, s.StorageLocation, s.RackNumber, s.SafeNumber, s.StoredFrom })
            .ToListAsync(ct));

    [HttpGet("storage/customer/{customerId:guid}")]
    public async Task<ActionResult> ForCustomer(Guid customerId, CancellationToken ct) =>
        Ok(await db.StorageRecords
            .Where(s => s.Firearm!.CustomerId == customerId)
            .Select(s => new { s.Id, s.FirearmId, s.MonthlyRate, s.StorageStatus, s.StoredFrom, s.StoredUntil })
            .ToListAsync(ct));

    [HttpPost("firearms/{firearmId:guid}/storage")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Start(Guid firearmId, StartStorageRequest request, CancellationToken ct)
    {
        var firearmExists = await db.Firearms.AnyAsync(f => f.Id == firearmId, ct);
        if (!firearmExists)
        {
            return Problem(detail: "Firearm not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var record = new StorageRecord
        {
            FirearmId = firearmId,
            StoredFrom = request.StoredFrom,
            MonthlyRate = request.MonthlyRate,
            StorageStatus = StorageStatus.Active,
            StorageLocation = request.StorageLocation,
            RackNumber = request.RackNumber,
            SafeNumber = request.SafeNumber,
            Notes = request.Notes,
        };
        await db.StorageRecords.AddAsync(record, ct);
        await db.SaveChangesAsync(ct);

        return Created($"/api/v1/storage/{record.Id}", new { record.Id });
    }

    [HttpPatch("storage-records/{id:guid}/release")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Release(Guid id, ReleaseStorageRequest? request, CancellationToken ct)
    {
        var record = await db.StorageRecords.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (record is null)
        {
            return NotFound();
        }

        record.StoredUntil = request?.StoredUntil ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        record.StorageStatus = StorageStatus.Released;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    public sealed record StartStorageRequest(
        DateOnly StoredFrom, decimal MonthlyRate, string? StorageLocation, string? RackNumber, string? SafeNumber, string? Notes);

    public sealed record ReleaseStorageRequest(DateOnly? StoredUntil);
}
