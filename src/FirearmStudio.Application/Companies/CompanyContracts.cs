using System.Linq.Expressions;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Entities;

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
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static Expression<Func<Company, CompanyDetailsResponse>> QueryProjection => c => new CompanyDetailsResponse(
        c.Id, c.Name, c.RegistrationNumber, c.VatNumber, c.Email, c.Phone,
        c.AddressLine1, c.AddressLine2, c.City, c.Province, c.PostalCode,
        c.IsActive, c.CreatedAt, c.UpdatedAt);
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
    Optional<string?> PostalCode);
