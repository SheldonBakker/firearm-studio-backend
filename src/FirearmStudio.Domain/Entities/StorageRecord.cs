using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Entities;

public class StorageRecord : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid FirearmId { get; set; }

    public DateOnly StoredFrom { get; set; }
    public DateOnly? StoredUntil { get; set; }

    public decimal MonthlyRate { get; set; }

    public StorageStatus StorageStatus { get; set; } = StorageStatus.Active;

    public string? StorageLocation { get; set; }
    public string? RackNumber { get; set; }
    public string? SafeNumber { get; set; }

    public string? Notes { get; set; }

    public Firearm? Firearm { get; set; }
}
