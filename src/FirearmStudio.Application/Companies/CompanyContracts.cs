using System.Linq.Expressions;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Companies;

public sealed record CompanyDetailsResponse(
    Guid Id,
    string Name,
    string? RegistrationNumber,
    string? VatNumber,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Province,
    string? PostalCode,
    string? BankName,
    string? BankAccountHolder,
    string? BankAccountNumber,
    string? BankBranchCode,
    string? BankAccountType,
    string? BankSwiftCode,
    bool IsActive,
    DepositMode DepositMode,
    decimal DepositValue,
    int DepositWindowHours,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static Expression<Func<Company, CompanyDetailsResponse>> QueryProjection => c => new CompanyDetailsResponse(
        c.Id, c.Name, c.RegistrationNumber, c.VatNumber, c.Email, c.Phone,
        c.AddressLine1, c.AddressLine2, c.City, c.Province, c.PostalCode,
        c.BankName, c.BankAccountHolder, c.BankAccountNumber, c.BankBranchCode, c.BankAccountType, c.BankSwiftCode,
        c.IsActive, c.DepositMode, c.DepositValue, c.DepositWindowHours, c.CreatedAt, c.UpdatedAt);
}

public sealed record UpdateCompanyRequest(
    Optional<string> Name,
    Optional<string?> RegistrationNumber,
    Optional<string?> VatNumber,
    Optional<string?> Email,
    Optional<string?> Phone,
    Optional<string?> AddressLine1,
    Optional<string?> AddressLine2,
    Optional<string?> City,
    Optional<string?> Province,
    Optional<string?> PostalCode,
    Optional<string?> BankName,
    Optional<string?> BankAccountHolder,
    Optional<string?> BankAccountNumber,
    Optional<string?> BankBranchCode,
    Optional<string?> BankAccountType,
    Optional<string?> BankSwiftCode,
    Optional<DepositMode> DepositMode,
    Optional<decimal> DepositValue,
    Optional<int> DepositWindowHours);
