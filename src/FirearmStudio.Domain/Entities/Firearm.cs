using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Entities;

public sealed class Firearm : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid CustomerId { get; set; }

    public required string Make { get; set; }
    public string? Model { get; set; }
    public string? Calibre { get; set; }
    public string? FirearmType { get; set; }

    public required string SerialNumber { get; set; }

    public FirearmStatus Status { get; set; } = FirearmStatus.InStorage;

    public string? InternalReference { get; set; }
    public string? Notes { get; set; }

    public Customer? Customer { get; set; }
    public ICollection<FirearmLicence> Licences { get; set; } = [];
    public ICollection<StorageRecord> StorageRecords { get; set; } = [];
}
