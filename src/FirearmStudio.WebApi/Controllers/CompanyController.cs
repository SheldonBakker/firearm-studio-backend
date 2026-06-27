using Asp.Versioning;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Companies;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/company")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class CompanyController(
    IApplicationDbContext db,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CompanyDetailsResponse>> Get(CancellationToken ct)
    {
        if (currentUserService.User.CompanyId is not { } companyId)
        {
            return NotFound();
        }

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct);
        return company is null ? NotFound() : ToResponse(company);
    }

    [HttpPatch]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult> Update(UpdateCompanyRequest request, CancellationToken ct)
    {
        if (currentUserService.User.CompanyId is not { } companyId)
        {
            return NotFound();
        }

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (company is null)
        {
            return NotFound();
        }

        company.Name = request.Name ?? company.Name;
        company.RegistrationNumber = request.RegistrationNumber ?? company.RegistrationNumber;
        company.VatNumber = request.VatNumber ?? company.VatNumber;
        company.Email = request.Email ?? company.Email;
        company.Phone = request.Phone ?? company.Phone;
        company.AddressLine1 = request.AddressLine1 ?? company.AddressLine1;
        company.AddressLine2 = request.AddressLine2 ?? company.AddressLine2;
        company.City = request.City ?? company.City;
        company.Province = request.Province ?? company.Province;
        company.PostalCode = request.PostalCode ?? company.PostalCode;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static CompanyDetailsResponse ToResponse(Company c) =>
        new(c.Id, c.Name, c.RegistrationNumber, c.VatNumber, c.Email, c.Phone,
            c.AddressLine1, c.AddressLine2, c.City, c.Province, c.PostalCode,
            c.IsActive, c.CreatedAt, c.UpdatedAt);
}
