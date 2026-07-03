using System.Linq.Expressions;
using FirearmStudio.Application.Customers;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Invoices;

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
    CustomerResponse? Customer,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<InvoicePaymentDto> Payments)
{
    public static Expression<Func<Invoice, InvoiceDetailDto>> QueryProjection => invoice => new InvoiceDetailDto(
        invoice.Id,
        invoice.CustomerId,
        invoice.InvoiceNumber,
        invoice.InvoiceMonth,
        invoice.Subtotal,
        invoice.VatAmount,
        invoice.Total,
        invoice.Status,
        invoice.SentAt,
        invoice.DueOn,
        invoice.Customer == null
            ? null
            : new CustomerResponse(
                invoice.Customer.Id,
                invoice.Customer.CustomerType,
                invoice.Customer.FullName,
                invoice.Customer.CompanyName,
                invoice.Customer.Email,
                invoice.Customer.Phone,
                invoice.Customer.Notes,
                invoice.Customer.IsActive),
        invoice.Lines
            .Select(line => new InvoiceLineDto(
                line.Id,
                line.Description,
                line.Quantity,
                line.UnitPrice,
                line.LineTotal))
            .ToList(),
        invoice.Payments
            .Select(payment => new InvoicePaymentDto(
                payment.Id,
                payment.Amount,
                payment.PaidOn,
                payment.Method,
                payment.Reference))
            .ToList());
}
