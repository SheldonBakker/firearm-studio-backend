using System.Linq.Expressions;
using FirearmStudio.Application.Model;
using FirearmStudio.Application.StorageRecords;
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

public sealed record CustomerDetailResponse(
    Guid Id,
    CustomerType CustomerType,
    string? FullName,
    string? CompanyName,
    string? Email,
    string? Phone,
    string? Notes,
    bool IsActive,
    IReadOnlyList<CustomerFirearmListItemDto> Firearms,
    IReadOnlyList<CustomerInvoiceListItemDto> Invoices,
    IReadOnlyList<CustomerStorageRecordDto> StorageRecords)
{
    public static Expression<Func<Customer, CustomerDetailResponse>> QueryProjection => c => new CustomerDetailResponse(
        c.Id, c.CustomerType, c.FullName, c.CompanyName, c.Email, c.Phone, c.Notes, c.IsActive,
        c.Firearms
            .OrderBy(f => f.SerialNumber)
            .ThenBy(f => f.Id)
            .Select(f => new CustomerFirearmListItemDto(f.Id, f.Make, f.Model, f.SerialNumber, f.Status))
            .ToList(),
        c.Invoices
            .OrderByDescending(i => i.InvoiceMonth)
            .ThenBy(i => i.Id)
            .Select(i => new CustomerInvoiceListItemDto(i.Id, i.InvoiceNumber, i.InvoiceMonth, i.Total, i.Status))
            .ToList(),
        c.Firearms
            .SelectMany(f => f.StorageRecords)
            .OrderByDescending(s => s.StoredFrom)
            .ThenBy(s => s.Id)
            .Select(s => new CustomerStorageRecordDto(s.Id, s.FirearmId, s.MonthlyRate, s.StorageStatus, s.StoredFrom, s.StoredUntil))
            .ToList());
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
    Optional<string> FullName,
    Optional<string?> CompanyName,
    Optional<string> Email,
    Optional<string> Phone,
    Optional<string?> Notes,
    Optional<bool> IsActive);
