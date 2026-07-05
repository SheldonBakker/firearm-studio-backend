using FirearmStudio.Domain.Common;

namespace FirearmStudio.Domain.Entities;

public sealed class RangeOperatingHours : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid ShootingRangeId { get; set; }

    public DayOfWeek Day { get; set; }

    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }

    public ShootingRange? ShootingRange { get; set; }
}
