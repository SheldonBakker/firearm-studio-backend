using Asp.Versioning;
using FirearmStudio.Application.Invoices;
using FirearmStudio.Application.Invoices.CancelInvoice;
using FirearmStudio.Application.Invoices.GenerateMonthlyInvoices;
using FirearmStudio.Application.Invoices.GetInvoice;
using FirearmStudio.Application.Invoices.GetInvoices;
using FirearmStudio.Application.Invoices.RecordPayment;
using FirearmStudio.Application.Invoices.SendInvoice;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/invoices")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class InvoicesController(IMediator mediator) : ControllerBase
{
    [HttpPost("generate-monthly")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> GenerateMonthly(GenerateMonthlyInvoicesRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new GenerateMonthlyInvoicesCommand(request), ct);
        return result.ToActionResult();
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceListItemDto>>> List(CancellationToken ct)
    {
        var result = await mediator.Send(new GetInvoicesQuery(), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInvoiceQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/send")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Send(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new SendInvoiceCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> AddPayment(Guid id, RecordPaymentRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new RecordPaymentCommand(id, request), ct);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelInvoiceCommand(id), ct);
        return result.ToActionResult();
    }
}
