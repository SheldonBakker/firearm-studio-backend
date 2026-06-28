using System.Linq.Expressions;
using ErrorOr;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Invoices;

public sealed record GenerateMonthlyInvoicesRequest(DateOnly InvoiceMonth, decimal VatRate, int DueDays);

public sealed record GenerateMonthlyInvoicesResponse(int InvoicesCreated, int InvoicesSkipped);

public interface IInvoiceGenerationService
{
    Task<ErrorOr<GenerateMonthlyInvoicesResponse>> GenerateMonthlyAsync(
        GenerateMonthlyInvoicesRequest request, CancellationToken ct = default);
}

public sealed record RecordPaymentRequest(decimal Amount, DateOnly? PaidOn, PaymentMethod Method, string? Reference, string? Notes);

public sealed record InvoiceListItemDto(
    Guid Id,
    Guid CustomerId,
    string InvoiceNumber,
    DateOnly InvoiceMonth,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total,
    InvoiceStatus Status,
    DateOnly? DueOn)
{
    public static Expression<Func<Invoice, InvoiceListItemDto>> QueryProjection => i => new InvoiceListItemDto(
        i.Id, i.CustomerId, i.InvoiceNumber, i.InvoiceMonth, i.Subtotal, i.VatAmount, i.Total, i.Status, i.DueOn);
}

public sealed record InvoiceLineDto(Guid Id, string Description, decimal Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record InvoicePaymentDto(Guid Id, decimal Amount, DateOnly PaidOn, PaymentMethod Method, string? Reference);

public sealed record InvoiceDetailDto(
    Guid Id,
    Guid CustomerId,
    string InvoiceNumber,
    DateOnly InvoiceMonth,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total,
    InvoiceStatus Status,
    DateTime? SentAt,
    DateOnly? DueOn,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<InvoicePaymentDto> Payments);
