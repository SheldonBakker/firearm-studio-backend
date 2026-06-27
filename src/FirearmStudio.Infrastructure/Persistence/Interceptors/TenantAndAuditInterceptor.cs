using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FirearmStudio.Infrastructure.Persistence.Interceptors;

public sealed class TenantAndAuditInterceptor(ITenantContext tenant) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.Id == Guid.Empty)
                    {
                        entry.Entity.Id = Guid.CreateVersion7();
                    }

                    entry.Entity.CreatedAt = now;
                    StampTenantOnInsert(entry);
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    GuardTenantNotChanged(entry);
                    break;
            }
        }
    }

    private void StampTenantOnInsert(EntityEntry<BaseEntity> entry)
    {
        if (entry.Entity is not ITenantEntity tenantEntity)
        {
            return;
        }

        if (tenantEntity.CompanyId != Guid.Empty)
        {
            return;
        }

        if (tenant.CompanyId is { } companyId)
        {
            tenantEntity.CompanyId = companyId;
        }
        else if (!tenant.BypassFilter)
        {
            throw new InvalidOperationException(
                $"Cannot insert tenant entity '{entry.Entity.GetType().Name}' without a current company.");
        }
    }

    private static void GuardTenantNotChanged(EntityEntry<BaseEntity> entry)
    {
        if (entry.Entity is not ITenantEntity)
        {
            return;
        }

        var companyIdProp = entry.Property(nameof(ITenantEntity.CompanyId));
        if (companyIdProp.IsModified &&
            !Equals(companyIdProp.OriginalValue, companyIdProp.CurrentValue))
        {
            throw new InvalidOperationException(
                $"Changing CompanyId on '{entry.Entity.GetType().Name}' is not allowed (cross-tenant move).");
        }
    }
}
