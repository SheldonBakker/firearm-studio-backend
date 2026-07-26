using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Entities;

public sealed class LicenceReminder : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid LicenceId { get; set; }

    public LicenceReminderTier Tier { get; set; }

    public FirearmLicence? Licence { get; set; }
}
