using FirearmStudio.Domain.Common;

namespace FirearmStudio.Domain.Entities;

public sealed class OutboxMessage : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public required string Type { get; set; }

    public required string Payload { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int Attempts { get; set; }

    public string? Error { get; set; }

    public DateTime? LockedUntil { get; set; }
}
