using FirearmStudio.Application.Bookings;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Abstractions.Email;

public sealed record CompanyEmailDetails(
    string? Name, string? Email, string? Phone,
    string? BankName, string? BankAccountHolder, string? BankAccountNumber,
    string? BankBranchCode, string? BankAccountType)
{
    public static readonly CompanyEmailDetails Empty =
        new(null, null, null, null, null, null, null, null);

    public static CompanyEmailDetails From(Company company) =>
        new(company.Name, company.Email, company.Phone,
            company.BankName, company.BankAccountHolder, company.BankAccountNumber,
            company.BankBranchCode, company.BankAccountType);

    internal static CompanyEmailDetails From(CompanyNotificationData data) =>
        new(data.Name, data.Email, data.Phone,
            data.BankName, data.BankAccountHolder, data.BankAccountNumber,
            data.BankBranchCode, data.BankAccountType);
}
