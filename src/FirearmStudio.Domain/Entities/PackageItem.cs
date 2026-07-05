using FirearmStudio.Domain.Common;

namespace FirearmStudio.Domain.Entities;

public sealed class PackageItem : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid PackageId { get; set; }

    public required string Description { get; set; }

    public decimal Quantity { get; set; } = 1;

    public int SortOrder { get; set; }

    public Package? Package { get; set; }
}
