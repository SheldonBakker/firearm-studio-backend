using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Entities;

public sealed class Invoice : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid CustomerId { get; set; }

    public required string InvoiceNumber { get; set; }

    public DateOnly InvoiceMonth { get; set; }

    public decimal Subtotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public InvoiceKind Kind { get; set; } = InvoiceKind.MonthlyStorage;

    public DateTime? SentAt { get; set; }
    public DateOnly? DueOn { get; set; }

    public decimal? DepositAmount { get; set; }
    public DateTime? DepositDueAt { get; set; }
    public DateTime? DepositPaidAt { get; set; }

    public Customer? Customer { get; set; }
    public ICollection<InvoiceLine> Lines { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
