using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Entities;

public class Payment : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid InvoiceId { get; set; }

    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Eft;

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Invoice? Invoice { get; set; }
}
