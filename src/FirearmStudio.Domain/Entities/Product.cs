using FirearmStudio.Domain.Common;

namespace FirearmStudio.Domain.Entities;

public sealed class Product : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Sku { get; set; }

    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }

    public string? Category { get; set; }
    public int StockQuantity { get; set; }

    public string? ImageKey { get; set; }

    public bool IsActive { get; set; } = true;
}
