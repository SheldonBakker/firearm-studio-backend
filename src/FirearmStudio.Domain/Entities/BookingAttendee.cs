using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Entities;

public sealed class BookingAttendee : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid BookingId { get; set; }

    public required string FullName { get; set; }
    public required string IdNumber { get; set; }

    public string? LicenceNumber { get; set; }
    public string? FirearmMakeModel { get; set; }
    public string? FirearmSerialNumber { get; set; }
    public string? Calibre { get; set; }

    public FirearmOrigin FirearmOrigin { get; set; } = FirearmOrigin.Own;

    public bool SignedIndemnity { get; set; }

    public string? Notes { get; set; }

    public Booking? Booking { get; set; }
}
