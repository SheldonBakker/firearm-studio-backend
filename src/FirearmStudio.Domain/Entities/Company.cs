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

    public bool IsActive { get; set; } = true;

    public ICollection<AppUser> Users { get; set; } = [];
}
