using ErrorOr;

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

public interface IOnboardingService
{
    Task<ErrorOr<CompanyResponse>> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken ct = default);
}
