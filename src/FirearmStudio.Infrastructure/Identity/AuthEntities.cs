using FirearmStudio.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace FirearmStudio.Infrastructure.Identity;

public sealed class AppIdentityUser : IdentityUser<Guid>
{
    // E.164, holds a phone number awaiting confirmation during the phone-change flow.
    public string? PendingPhoneNumber { get; set; }
}

public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid? ReplacedById { get; set; }
}

public sealed class OtpCode
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public OtpPurpose Purpose { get; set; }

    public required string CodeHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public int AttemptCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
