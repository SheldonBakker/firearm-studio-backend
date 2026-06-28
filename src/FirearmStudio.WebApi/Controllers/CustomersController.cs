using Asp.Versioning;
using FirearmStudio.Application.Customers;
using FirearmStudio.Application.Customers.CreateCustomer;
using FirearmStudio.Application.Customers.GetCustomer;
using FirearmStudio.Application.Customers.GetCustomerFirearms;
using FirearmStudio.Application.Customers.GetCustomerInvoices;
using FirearmStudio.Application.Customers.GetCustomers;
using FirearmStudio.Application.Customers.UpdateCustomer;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/customers")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class CustomersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CustomerListItemDto>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? name = null,
        [FromQuery] string? email = null,
        [FromQuery] string? phone = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCustomersQuery(pageNumber, pageSize, sortOrder, name, email, phone), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> Get(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCustomerQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/firearms")]
    public async Task<ActionResult<IReadOnlyList<CustomerFirearmListItemDto>>> Firearms(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCustomerFirearmsQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/invoices")]
    public async Task<ActionResult<IReadOnlyList<CustomerInvoiceListItemDto>>> Invoices(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCustomerInvoicesQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateCustomerCommand(request), ct);
        return result.IsError
            ? result.ToActionResult()
            : CreatedAtAction(nameof(Get), new { id = result.Value.Id, version = "1" }, result.Value);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Update(Guid id, UpdateCustomerRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateCustomerCommand(id, request), ct);
        return result.ToActionResult();
    }
}
