using FirearmStudio.Domain.Common;

namespace FirearmStudio.Domain.Entities;

public sealed class ShootingRange : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }

    public int LaneCount { get; set; }

    public int SlotIntervalMinutes { get; set; } = 30;

    public bool IsActive { get; set; } = true;

    public ICollection<RangeOperatingHours> OperatingHours { get; set; } = [];
}
