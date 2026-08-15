using FirearmStudio.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Auth;

internal static class AppUserLinker
{
    public static async Task LinkAsync(
        IApplicationDbContext db,
        ITenantContext tenant,
        Guid authUserId,
        string email,
        CancellationToken ct)
    {
        using (tenant.BeginBypass())
        {
            var pending = await db.AppUsers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    u => u.Email == email && u.AuthUserId == null && u.IsActive,
                    ct);

            if (pending is null)
            {
                return;
            }

            pending.AuthUserId = authUserId;
            pending.LinkedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
        }
    }
}
