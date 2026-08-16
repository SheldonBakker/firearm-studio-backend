namespace FirearmStudio.Application.Onboarding;

public sealed record CreateCompanyRequest(
    string Name,
    string? RegistrationNumber,
    string? VatNumber,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Province,
    string? PostalCode);

public sealed record CompanyResponse(Guid Id, string Name);

public sealed record CreateCompanyOnboardingResponse(
    CompanyResponse Company,
    string Message = "Company created and you are its admin. Call /auth/refresh to receive your company_id and admin role in a new access token.");
