using FirearmStudio.Domain.Common;

namespace FirearmStudio.Domain.Entities;

public sealed class InvoiceLine : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid InvoiceId { get; set; }
    public Guid? FirearmId { get; set; }

    public required string Description { get; set; }

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public Invoice? Invoice { get; set; }
    public Firearm? Firearm { get; set; }
}
