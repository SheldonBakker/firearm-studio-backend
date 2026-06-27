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
[Route("api/v{version:apiVersion}/customers")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class CustomersController(IApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> List(CancellationToken ct) =>
        await db.Customers.OrderBy(c => c.FullName)
            .Select(c => new CustomerResponse(c.Id, c.CustomerType, c.FullName, c.CompanyName, c.Email, c.Phone, c.IsActive))
            .ToListAsync(ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> Get(Guid id, CancellationToken ct)
    {
        var c = await db.Customers.FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? NotFound() : ToResponse(c);
    }

    [HttpGet("{id:guid}/firearms")]
    public async Task<ActionResult> Firearms(Guid id, CancellationToken ct) =>
        Ok(await db.Firearms.Where(f => f.CustomerId == id)
            .Select(f => new { f.Id, f.Make, f.Model, f.SerialNumber, f.Status })
            .ToListAsync(ct));

    [HttpGet("{id:guid}/invoices")]
    public async Task<ActionResult> Invoices(Guid id, CancellationToken ct) =>
        Ok(await db.Invoices.Where(i => i.CustomerId == id)
            .Select(i => new { i.Id, i.InvoiceNumber, i.InvoiceMonth, i.Total, i.Status })
            .ToListAsync(ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var customer = new Customer
        {
            CustomerType = request.CustomerType,
            FullName = request.FullName,
            CompanyName = request.CompanyName,
            RegistrationNumber = request.RegistrationNumber,
            VatNumber = request.VatNumber,
            Email = request.Email,
            Phone = request.Phone,
            AddressLine1 = request.AddressLine1,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode,
            Notes = request.Notes,
        };
        await db.Customers.AddAsync(customer, ct);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = customer.Id, version = "1" }, ToResponse(customer));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Update(Guid id, UpdateCustomerRequest request, CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (customer is null)
        {
            return NotFound();
        }

        customer.FullName = request.FullName ?? customer.FullName;
        customer.CompanyName = request.CompanyName ?? customer.CompanyName;
        customer.Email = request.Email ?? customer.Email;
        customer.Phone = request.Phone ?? customer.Phone;
        customer.Notes = request.Notes ?? customer.Notes;
        if (request.IsActive is { } active)
        {
            customer.IsActive = active;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static CustomerResponse ToResponse(Customer c) =>
        new(c.Id, c.CustomerType, c.FullName, c.CompanyName, c.Email, c.Phone, c.IsActive);

    public sealed record CustomerResponse(
        Guid Id, CustomerType CustomerType, string? FullName, string? CompanyName, string? Email, string? Phone, bool IsActive);

    public sealed record CreateCustomerRequest(
        CustomerType CustomerType, string? FullName, string? CompanyName, string? RegistrationNumber,
        string? VatNumber, string? Email, string? Phone, string? AddressLine1, string? City,
        string? Province, string? PostalCode, string? Notes);

    public sealed record UpdateCustomerRequest(
        string? FullName, string? CompanyName, string? Email, string? Phone, string? Notes, bool? IsActive);
}
