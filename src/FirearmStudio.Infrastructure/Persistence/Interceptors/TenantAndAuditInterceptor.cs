using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FirearmStudio.Infrastructure.Persistence.Interceptors;

public sealed class TenantAndAuditInterceptor(
    ITenantContext tenant,
    ICurrentUserService currentUserService) : SaveChangesInterceptor
{
    private static readonly HashSet<Type> AuditedTypes =
    [
        typeof(Customer),
        typeof(Firearm),
        typeof(StorageRecord),
        typeof(Invoice),
        typeof(Payment),
    ];

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
        var auditLogs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>().ToList())
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

            if (AuditedTypes.Contains(entry.Entity.GetType()) &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                auditLogs.Add(BuildAuditLog(entry));
            }
        }

        if (tenant.CompanyId is not { } companyId)
        {
            return;
        }

        var appUserId = currentUserService.User.IsAuthenticated
            ? ResolveAppUserId(context, currentUserService.User.Id)
            : null;

        foreach (var log in auditLogs)
        {
            log.Id = Guid.CreateVersion7();
            log.CreatedAt = now;
            log.CompanyId = companyId;
            log.AppUserId = appUserId;
            context.Add(log);
        }
    }

    private bool _appUserResolved;
    private Guid? _appUserId;

    private Guid? ResolveAppUserId(DbContext context, Guid authUserId)
    {
        if (_appUserResolved)
        {
            return _appUserId;
        }

        // The AppUser tenant query filter scopes this to the current company.
        _appUserId = context.Set<AppUser>()
            .Where(u => u.AuthUserId == authUserId)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefault();
        _appUserResolved = true;
        return _appUserId;
    }

    private static AuditLog BuildAuditLog(EntityEntry<BaseEntity> entry)
    {
        var props = entry.Properties.Where(p => !p.Metadata.IsPrimaryKey());

        string? oldValue = null;
        string? newValue = null;

        if (entry.State is EntityState.Modified or EntityState.Deleted)
        {
            oldValue = JsonSerializer.Serialize(
                props.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
        }

        if (entry.State is not EntityState.Deleted)
        {
            newValue = JsonSerializer.Serialize(
                props.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
        }

        var action = entry.State switch
        {
            EntityState.Added => "Created",
            EntityState.Deleted => "Deleted",
            _ => "Updated",
        };

        return new AuditLog
        {
            EntityType = entry.Entity.GetType().Name,
            EntityId = entry.Entity.Id,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
        };
    }

    private void StampTenantOnInsert(EntityEntry<BaseEntity> entry)
    {
        if (entry.Entity is not ITenantEntity tenantEntity)
        {
            return;
        }

        if (tenantEntity.CompanyId != Guid.Empty)
        {
            if (!tenant.BypassFilter && tenantEntity.CompanyId != tenant.CompanyId)
            {
                throw new InvalidOperationException(
                    $"Cannot insert tenant entity '{entry.Entity.GetType().Name}' for another company.");
            }

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

    private void GuardTenantNotChanged(EntityEntry<BaseEntity> entry)
    {
        if (entry.Entity is not ITenantEntity)
        {
            return;
        }

        // An explicit BeginBypass() scope authorises deliberate tenant moves (reassignment / onboarding).
        if (tenant.BypassFilter)
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
