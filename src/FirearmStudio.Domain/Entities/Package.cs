using FirearmStudio.Domain.Common;

namespace FirearmStudio.Domain.Entities;

public sealed class Package : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int DurationMinutes { get; set; }

    public int MaxShooters { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public ICollection<PackageItem> Items { get; set; } = [];
}
