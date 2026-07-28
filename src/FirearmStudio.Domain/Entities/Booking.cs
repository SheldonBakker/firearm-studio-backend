using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Entities;

public sealed class Booking : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid ShootingRangeId { get; set; }
    public Guid PackageId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? InvoiceId { get; set; }

    public required string BookingNumber { get; set; }

    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public BookingSource Source { get; set; }

    public required string PackageName { get; set; }
    public decimal PackagePrice { get; set; }

    public int ShooterCount { get; set; } = 1;

    public string? Notes { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public required string CalendarToken { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public DateTime? CheckedInAt { get; set; }

    public ShootingRange? ShootingRange { get; set; }
    public Package? Package { get; set; }
    public Customer? Customer { get; set; }
    public Invoice? Invoice { get; set; }
}
