using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Abstractions.Email;

public sealed record CompanyEmailDetails(
    string? Name, string? Email, string? Phone,
    string? BankName, string? BankAccountHolder, string? BankAccountNumber,
    string? BankBranchCode, string? BankAccountType)
{
    public static CompanyEmailDetails From(Company company) =>
        new(company.Name, company.Email, company.Phone,
            company.BankName, company.BankAccountHolder, company.BankAccountNumber,
            company.BankBranchCode, company.BankAccountType);
}
