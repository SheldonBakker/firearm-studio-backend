using Asp.Versioning;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class LicencesController(IApplicationDbContext db) : ControllerBase
{
    [HttpGet("licences/due-renewal")]
    public async Task<ActionResult> DueForRenewal(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var horizon = today.AddDays(30);
        return Ok(await db.FirearmLicences
            .Where(l => l.RenewalDueOn >= today && l.RenewalDueOn <= horizon)
            .OrderBy(l => l.RenewalDueOn)
            .Select(l => new { l.Id, l.FirearmId, l.LicenceNumber, l.ExpiresOn, l.RenewalDueOn, l.Status })
            .ToListAsync(ct));
    }

    [HttpGet("licences/expired")]
    public async Task<ActionResult> Expired(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return Ok(await db.FirearmLicences
            .Where(l => l.ExpiresOn < today)
            .OrderBy(l => l.ExpiresOn)
            .Select(l => new { l.Id, l.FirearmId, l.LicenceNumber, l.ExpiresOn, l.Status })
            .ToListAsync(ct));
    }

    [HttpPost("firearms/{firearmId:guid}/licences")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Create(Guid firearmId, CreateLicenceRequest request, CancellationToken ct)
    {
        var firearmExists = await db.Firearms.AnyAsync(f => f.Id == firearmId, ct);
        if (!firearmExists)
        {
            return Problem(detail: "Firearm not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var licence = new FirearmLicence
        {
            FirearmId = firearmId,
            LicenceNumber = request.LicenceNumber,
            IssuedOn = request.IssuedOn,
            ExpiresOn = request.ExpiresOn,
            Status = LicenceStatus.Valid,
            DocumentUrl = request.DocumentUrl,
        };
        await db.FirearmLicences.AddAsync(licence, ct);
        await db.SaveChangesAsync(ct);

        return Created($"/api/v1/firearms/{firearmId}/licences", new { licence.Id });
    }

    [HttpPatch("licences/{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Update(Guid id, UpdateLicenceRequest request, CancellationToken ct)
    {
        var licence = await db.FirearmLicences.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (licence is null)
        {
            return NotFound();
        }

        licence.LicenceNumber = request.LicenceNumber ?? licence.LicenceNumber;
        licence.IssuedOn = request.IssuedOn ?? licence.IssuedOn;
        if (request.ExpiresOn is { } expires)
        {
            licence.ExpiresOn = expires;
        }
        if (request.Status is { } status)
        {
            licence.Status = status;
        }
        licence.DocumentUrl = request.DocumentUrl ?? licence.DocumentUrl;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    public sealed record CreateLicenceRequest(string LicenceNumber, DateOnly? IssuedOn, DateOnly ExpiresOn, string? DocumentUrl);

    public sealed record UpdateLicenceRequest(string? LicenceNumber, DateOnly? IssuedOn, DateOnly? ExpiresOn, LicenceStatus? Status, string? DocumentUrl);
}
