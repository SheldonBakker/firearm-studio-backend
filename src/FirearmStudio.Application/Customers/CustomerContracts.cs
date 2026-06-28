using System.Linq.Expressions;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Customers;

public sealed record CustomerResponse(
    Guid Id,
    CustomerType CustomerType,
    string? FullName,
    string? CompanyName,
    string? Email,
    string? Phone,
    string? Notes,
    bool IsActive)
{
    public static Expression<Func<Customer, CustomerResponse>> QueryProjection => c => new CustomerResponse(
        c.Id, c.CustomerType, c.FullName, c.CompanyName, c.Email, c.Phone, c.Notes, c.IsActive);

    public static CustomerResponse FromEntity(Customer c) =>
        new(c.Id, c.CustomerType, c.FullName, c.CompanyName, c.Email, c.Phone, c.Notes, c.IsActive);
}

public sealed record CustomerListItemDto(
    Guid Id,
    CustomerType CustomerType,
    string? FullName,
    string? CompanyName,
    string? Email,
    string? Phone,
    bool IsActive,
    DateTime CreatedAt)
{
    public static Expression<Func<Customer, CustomerListItemDto>> QueryProjection => c => new CustomerListItemDto(
        c.Id, c.CustomerType, c.FullName, c.CompanyName, c.Email, c.Phone, c.IsActive, c.CreatedAt);
}

public sealed record CustomerFirearmListItemDto(Guid Id, string? Make, string? Model, string? SerialNumber, FirearmStatus Status)
{
    public static Expression<Func<Firearm, CustomerFirearmListItemDto>> QueryProjection => f => new CustomerFirearmListItemDto(
        f.Id, f.Make, f.Model, f.SerialNumber, f.Status);
}

public sealed record CustomerInvoiceListItemDto(Guid Id, string? InvoiceNumber, DateOnly InvoiceMonth, decimal Total, InvoiceStatus Status)
{
    public static Expression<Func<Invoice, CustomerInvoiceListItemDto>> QueryProjection => i => new CustomerInvoiceListItemDto(
        i.Id, i.InvoiceNumber, i.InvoiceMonth, i.Total, i.Status);
}

public sealed record CreateCustomerRequest(
    CustomerType CustomerType, string? FullName, string? CompanyName, string? RegistrationNumber,
    string? VatNumber, string? Email, string? Phone, string? AddressLine1, string? City,
    string? Province, string? PostalCode, string? Notes);

public sealed record UpdateCustomerRequest(
    string? FullName, string? CompanyName, string? Email, string? Phone, string? Notes, bool? IsActive);
