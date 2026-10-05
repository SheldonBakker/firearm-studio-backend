using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Companies;

internal static class StorefrontAudit
{
    public const string Rotated = "StorefrontKeyRotated";
    public const string Revoked = "StorefrontKeyRevoked";

    public static async Task AddAsync(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        Guid companyId,
        string action,
        string? keySuffix,
        CancellationToken ct)
    {
        var appUserId = await db.AppUsers
            .Where(u => u.AuthUserId == currentUser.User.Id)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);

        db.AuditLogs.Add(new AuditLog
        {
            EntityType = nameof(Company),
            EntityId = companyId,
            Action = action,
            NewValue = JsonSerializer.Serialize(new { keySuffix }),
            AppUserId = appUserId,
        });
    }
}
