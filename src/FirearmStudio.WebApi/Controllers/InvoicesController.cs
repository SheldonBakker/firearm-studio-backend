using Asp.Versioning;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Invoices;
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
[Route("api/v{version:apiVersion}/invoices")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class InvoicesController(
    IApplicationDbContext db,
    IInvoiceGenerationService invoiceGeneration) : ControllerBase
{
    [HttpPost("generate-monthly")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> GenerateMonthly(GenerateMonthlyInvoicesRequest request, CancellationToken ct)
    {
        var result = await invoiceGeneration.GenerateMonthlyAsync(request, ct);
        return result.IsError ? this.ToProblem(result) : Ok(result.Value);
    }

    [HttpGet]
    public async Task<ActionResult> List(CancellationToken ct) =>
        Ok(await db.Invoices.OrderByDescending(i => i.InvoiceMonth)
            .Select(i => new { i.Id, i.CustomerId, i.InvoiceNumber, i.InvoiceMonth, i.Subtotal, i.VatAmount, i.Total, i.Status, i.DueOn })
            .ToListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> Get(Guid id, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Where(i => i.Id == id)
            .Select(i => new
            {
                i.Id, i.CustomerId, i.InvoiceNumber, i.InvoiceMonth, i.Subtotal, i.VatAmount, i.Total, i.Status, i.SentAt, i.DueOn,
                Lines = i.Lines.Select(l => new { l.Id, l.Description, l.Quantity, l.UnitPrice, l.LineTotal }),
                Payments = i.Payments.Select(p => new { p.Id, p.Amount, p.PaidOn, p.Method, p.Reference }),
            })
            .FirstOrDefaultAsync(ct);

        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpPost("{id:guid}/send")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Send(Guid id, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
        {
            return NotFound();
        }

        if (invoice.Status is InvoiceStatus.Cancelled or InvoiceStatus.Paid)
        {
            return Problem(detail: $"Cannot send an invoice that is {invoice.Status}.",
                statusCode: StatusCodes.Status409Conflict);
        }

        invoice.Status = InvoiceStatus.Sent;
        invoice.SentAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> AddPayment(Guid id, RecordPaymentRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0)
        {
            return Problem(detail: "Payment amount must be greater than zero.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
        {
            return NotFound();
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return Problem(detail: "Cannot record a payment against a cancelled invoice.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var alreadyPaid = await db.Payments
            .Where(p => p.InvoiceId == id)
            .SumAsync(p => p.Amount, ct);

        await db.Payments.AddAsync(new Payment
        {
            InvoiceId = id,
            Amount = request.Amount,
            PaidOn = request.PaidOn ?? DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Method = request.Method,
            Reference = request.Reference,
            Notes = request.Notes,
        }, ct);

        if (alreadyPaid + request.Amount >= invoice.Total)
        {
            invoice.Status = InvoiceStatus.Paid;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
        {
            return NotFound();
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return Problem(detail: "Cannot cancel a paid invoice.", statusCode: StatusCodes.Status409Conflict);
        }

        invoice.Status = InvoiceStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    public sealed record RecordPaymentRequest(decimal Amount, DateOnly? PaidOn, PaymentMethod Method, string? Reference, string? Notes);
}
