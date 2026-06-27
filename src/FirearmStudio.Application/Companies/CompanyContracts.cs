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
    DateTime? UpdatedAt);

public sealed record UpdateCompanyRequest(
    string? Name,
    string? RegistrationNumber,
    string? VatNumber,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Province,
    string? PostalCode);
