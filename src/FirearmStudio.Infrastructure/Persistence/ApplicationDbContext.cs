using System.Data;
using System.Reflection;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
    public DbSet<ShootingRange> ShootingRanges => Set<ShootingRange>();
    public DbSet<RangeOperatingHours> RangeOperatingHours => Set<RangeOperatingHours>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackageItem> PackageItems => Set<PackageItem>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<SageConnection> SageConnections => Set<SageConnection>();

    public void ClearChangeTracker() => ChangeTracker.Clear();

    public async Task<long> NextBookingNumberAsync(CancellationToken cancellationToken = default)
        => await Database
            .SqlQuery<long>($"""SELECT nextval('booking_number_seq') AS "Value" """)
            .SingleAsync(cancellationToken);

    private const int SerializableAttempts = 3;
    private const string SerializationFailureSqlState = "40001";

    public async Task<bool> TryExecuteInSerializableTransactionAsync(
        Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= SerializableAttempts; attempt++)
        {
            await using var transaction = await Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);

            try
            {
                await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (Exception ex) when (IsSerializationFailure(ex))
            {
                await transaction.RollbackAsync(cancellationToken);
                ChangeTracker.Clear();
            }
        }

        return false;
    }

    private static bool IsSerializationFailure(Exception? exception)
    {
        while (exception is not null)
        {
            if (exception is PostgresException { SqlState: SerializationFailureSqlState })
            {
                return true;
            }

            exception = exception.InnerException;
        }

        return false;
    }

    private static readonly MethodInfo SetTenantFilterMethod =
        typeof(ApplicationDbContext).GetMethod(nameof(SetTenantFilter),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.HasSequence<long>("booking_number_seq");

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
