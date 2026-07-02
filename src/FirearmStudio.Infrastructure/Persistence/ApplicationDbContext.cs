using System.Reflection;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenant)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Firearm> Firearms => Set<Firearm>();
    public DbSet<FirearmLicence> FirearmLicences => Set<FirearmLicence>();
    public DbSet<StorageRecord> StorageRecords => Set<StorageRecord>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public void ClearChangeTracker() => ChangeTracker.Clear();

    private static readonly MethodInfo SetTenantFilterMethod =
        typeof(ApplicationDbContext).GetMethod(nameof(SetTenantFilter),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                SetTenantFilterMethod
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, [modelBuilder]);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => tenant.BypassFilter || e.CompanyId == tenant.CompanyId);
    }
}
