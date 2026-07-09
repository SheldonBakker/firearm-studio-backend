using FirearmStudio.Domain.Common;

namespace FirearmStudio.Domain.Entities;

public sealed class SageConnection : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public required string ApiKeyCiphertext { get; set; }
    public required string UsernameCiphertext { get; set; }
    public required string PasswordCiphertext { get; set; }

    public int SageCompanyId { get; set; }
    public required string SageCompanyName { get; set; }

    public DateTime LastValidatedAt { get; set; }
    public Guid LastRegisteredByAuthUserId { get; set; }
}
