using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Entities;

public class FirearmLicence : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid FirearmId { get; set; }

    public required string LicenceNumber { get; set; }

    public DateOnly? IssuedOn { get; set; }
    public DateOnly ExpiresOn { get; set; }

    public DateOnly RenewalDueOn { get; private set; }

    public LicenceStatus Status { get; set; } = LicenceStatus.Valid;

    public string? DocumentUrl { get; set; }

    public Firearm? Firearm { get; set; }
}
