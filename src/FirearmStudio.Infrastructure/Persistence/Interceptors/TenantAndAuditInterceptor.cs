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
        typeof(ShootingRange),
        typeof(Package),
        typeof(Booking),
        typeof(BookingAttendee),
    ];

    // Properties that must never be written to AuditLog.OldValue/NewValue: capability credentials
    // and sensitive identifiers that would otherwise be logged in plaintext.
    private static readonly Dictionary<Type, HashSet<string>> AuditExcludedProperties = new()
    {
        [typeof(Booking)] = [nameof(Booking.CalendarToken)],
        [typeof(BookingAttendee)] = [nameof(BookingAttendee.IdNumber)],
        [typeof(Customer)] = [nameof(Customer.IdNumberCiphertext)],
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null)
        {
            return base.SavingChanges(eventData, result);
        }

        var (now, entries) = ApplyStampsAndCollect(eventData.Context);

        if (tenant.CompanyId is { } companyId && entries is not null)
        {
            var appUserId = currentUserService.User.IsAuthenticated
                ? ResolveAppUserId(eventData.Context, currentUserService.User.Id)
                : null;

            WriteAuditLogs(eventData.Context, entries, now, companyId, appUserId);
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var (now, entries) = ApplyStampsAndCollect(eventData.Context);

        if (tenant.CompanyId is { } companyId && entries is not null)
        {
            var appUserId = currentUserService.User.IsAuthenticated
                ? await ResolveAppUserIdAsync(eventData.Context, currentUserService.User.Id, cancellationToken)
                : null;

            WriteAuditLogs(eventData.Context, entries, now, companyId, appUserId);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // Applies timestamps and tenant stamps; returns auditable entries without building logs yet.
    // Audit log construction is skipped here so we can short-circuit when CompanyId is null.
    private (DateTime Now, List<EntityEntry<BaseEntity>>? AuditEntries) ApplyStampsAndCollect(DbContext context)
    {
        var now = DateTime.UtcNow;
        List<EntityEntry<BaseEntity>>? auditEntries = null;

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
                auditEntries ??= [];
                auditEntries.Add(entry);
            }
        }

        return (now, auditEntries);
    }

    private static void WriteAuditLogs(
        DbContext context,
        List<EntityEntry<BaseEntity>> entries,
        DateTime now,
        Guid companyId,
        Guid? appUserId)
    {
        foreach (var entry in entries)
        {
            var log = BuildAuditLog(entry);
            log.Id = Guid.CreateVersion7();
            log.CreatedAt = now;
            log.CompanyId = companyId;
            log.AppUserId = appUserId;
            context.Add(log);
        }
    }

    private bool _appUserResolved;
    private Guid? _appUserId;

    // Sync path: blocking query is acceptable on the synchronous SaveChanges path.
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

    private bool _asyncAppUserResolved;
    private Guid? _asyncAppUserId;

    // Async path: uses FirstOrDefaultAsync to avoid blocking I/O on the thread pool.
    private async ValueTask<Guid?> ResolveAppUserIdAsync(DbContext context, Guid authUserId, CancellationToken cancellationToken)
    {
        if (_asyncAppUserResolved)
        {
            return _asyncAppUserId;
        }

        _asyncAppUserId = await context.Set<AppUser>()
            .Where(u => u.AuthUserId == authUserId)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        _asyncAppUserResolved = true;
        return _asyncAppUserId;
    }

    private static AuditLog BuildAuditLog(EntityEntry<BaseEntity> entry)
    {
        // Materialize once to avoid double enumeration when both old and new values are needed.
        var excluded = AuditExcludedProperties.GetValueOrDefault(entry.Entity.GetType());
        var props = entry.Properties
            .Where(p => !p.Metadata.IsPrimaryKey())
            .Where(p => excluded is null || !excluded.Contains(p.Metadata.Name))
            .ToList();

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
