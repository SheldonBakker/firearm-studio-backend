using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Entities;

public sealed class Customer : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public CustomerType CustomerType { get; set; } = CustomerType.Individual;

    public string? FullName { get; set; }

    public string? IdNumber { get; set; }

    public string? CompanyName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? VatNumber { get; set; }

    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Firearm> Firearms { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
}
