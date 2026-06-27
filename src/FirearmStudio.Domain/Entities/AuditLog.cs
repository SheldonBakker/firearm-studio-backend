using FirearmStudio.Domain.Common;

namespace FirearmStudio.Domain.Entities;

public class AuditLog : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid? AppUserId { get; set; }

    public required string EntityType { get; set; }
    public Guid EntityId { get; set; }

    public required string Action { get; set; }

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
