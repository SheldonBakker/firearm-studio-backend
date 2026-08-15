using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Entities;

public sealed class AppUser : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public Guid? AuthUserId { get; set; }

    public required string Email { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }

    public AppRole Role { get; set; } = AppRole.Viewer;

    public bool IsActive { get; set; } = true;

    public DateTime? InvitedAt { get; set; }
    public DateTime? LinkedAt { get; set; }

    public Company? Company { get; set; }
}
