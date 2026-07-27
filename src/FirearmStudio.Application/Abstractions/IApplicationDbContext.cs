using FirearmStudio.Application.Dashboard.GetDashboardStats;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<AppUser> AppUsers { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Firearm> Firearms { get; }
    DbSet<FirearmLicence> FirearmLicences { get; }
    DbSet<LicenceReminder> LicenceReminders { get; }
    DbSet<StorageRecord> StorageRecords { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLine> InvoiceLines { get; }
    DbSet<Payment> Payments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ShootingRange> ShootingRanges { get; }
    DbSet<RangeOperatingHours> RangeOperatingHours { get; }
    DbSet<Package> Packages { get; }
    DbSet<PackageItem> PackageItems { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<SageConnection> SageConnections { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }

    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<long> NextBookingNumberAsync(CancellationToken cancellationToken = default);

    Task<List<long>> NextBookingNumbersAsync(int count, CancellationToken cancellationToken = default);

    Task<List<OutboxMessageBatchRow>> ClaimOutboxBatchAsync(int batchSize, CancellationToken cancellationToken = default);

    Task MarkOutboxProcessedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the attempt counter, clears the lock, and stores <paramref name="error"/> for the
    /// given outbox message. <paramref name="error"/> is truncated to 4000 characters here to match
    /// the column constraint declared in <c>OutboxMessageConfiguration</c>.
    /// </summary>
    Task MarkOutboxFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);

    Task<bool> TryExecuteInSerializableTransactionAsync(
        Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

    void ClearChangeTracker();

    Task<DashboardStatsRow> GetDashboardStatsRowAsync(Guid companyId, CancellationToken cancellationToken = default);
}
