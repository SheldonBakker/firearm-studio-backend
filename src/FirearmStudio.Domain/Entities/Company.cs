using FirearmStudio.Domain.Common;

namespace FirearmStudio.Domain.Entities;

public sealed class Company : BaseEntity
{
    public required string Name { get; set; }

    public string? RegistrationNumber { get; set; }
    public string? VatNumber { get; set; }

    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }

    public string? BankName { get; set; }
    public string? BankAccountHolder { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankBranchCode { get; set; }
    public string? BankAccountType { get; set; }
    public string? BankSwiftCode { get; set; }

    public bool IsActive { get; set; } = true;

    public int DueDays { get; set; } = 30;
    public bool AutoBillingEnabled { get; set; } = true;

    public ICollection<AppUser> Users { get; set; } = [];
}
