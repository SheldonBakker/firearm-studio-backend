using System.Data;
using System.Reflection;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FirearmStudio.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantContext tenant)
    : DbContext(options), IApplicationDbContext
{
    private const int SerializableAttempts = 3;
    private const string TenantFilterName = "TenantFilter";

    private static readonly MethodInfo ApplyTenantFilterMethod =
        typeof(ApplicationDbContext).GetMethod(
            nameof(ApplyTenantFilter),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

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
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public void ClearChangeTracker() => ChangeTracker.Clear();

    public Task<long> NextBookingNumberAsync(
        CancellationToken cancellationToken = default) =>
        Database
            .SqlQuery<long>(
                $"""SELECT nextval('booking_number_seq') AS "Value" """)
            .SingleAsync(cancellationToken);

    public async Task<bool> TryExecuteInSerializableTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < SerializableAttempts; attempt++)
        {
            await using var transaction =
                await Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            try
            {
                await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch (Exception exception) when (
                exception.GetBaseException() is PostgresException
                {
                    SqlState: PostgresErrorCodes.SerializationFailure
                })
            {
                ChangeTracker.Clear();
            }
        }

        return false;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasSequence<long>("booking_number_seq");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model
                     .GetEntityTypes()
                     .Where(static entityType =>
                         entityType.BaseType is null &&
                         typeof(ITenantEntity).IsAssignableFrom(
                             entityType.ClrType)))
        {
            ApplyTenantFilterMethod
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(
            TenantFilterName,
            entity => tenant.BypassFilter ||
                      entity.CompanyId == tenant.CompanyId);
}