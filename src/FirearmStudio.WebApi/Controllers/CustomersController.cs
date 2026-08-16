using FirearmStudio.Application.Customers;
using FirearmStudio.Application.Customers.CreateCustomer;
using FirearmStudio.Application.Customers.GetCustomer;
using FirearmStudio.Application.Customers.GetCustomers;
using FirearmStudio.Application.Customers.UpdateCustomer;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/customers")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class CustomersController(IMediator mediator) : ApiControllerBase
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
    public async Task<ActionResult<CustomerDetailResponse>> Get(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCustomerQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateCustomerCommand(request), ct);
        return result.IsError
            ? result.ToActionResult()
            : CreatedAtAction(nameof(Get), new { id = result.Value.Id, version = CurrentApiVersion }, result.Value);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Update(Guid id, UpdateCustomerRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateCustomerCommand(id, request), ct);
        return result.ToActionResult();
    }
}
