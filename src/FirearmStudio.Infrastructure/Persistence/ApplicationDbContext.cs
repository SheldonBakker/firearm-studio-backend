using System.Data;
using System.Reflection;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Dashboard.GetDashboardStats;
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
    private const int RetryBaseDelayMs = 20;
    private const int RetryJitterMs = 30;
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
    public DbSet<LicenceReminder> LicenceReminders => Set<LicenceReminder>();
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

    public Task<List<long>> NextBookingNumbersAsync(
        int count, CancellationToken cancellationToken = default) =>
        Database
            .SqlQuery<long>(
                $"""SELECT nextval('booking_number_seq') AS "Value" FROM generate_series(1, {count}) """)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Claims up to <paramref name="batchSize"/> pending outbox messages with <c>FOR UPDATE SKIP LOCKED</c>
    /// so that concurrent instances each receive a distinct batch.
    /// <c>attempts &lt; <see cref="OutboxMessageTypes.MaxAttempts"/></c> is inlined as a literal
    /// because it is a compile-time constant and Npgsql treats it as a safe literal, not user input.
    /// </summary>
    public async Task<List<OutboxMessageBatchRow>> ClaimOutboxBatchAsync(
        int batchSize, CancellationToken cancellationToken = default)
    {
        var connection = Database.GetDbConnection();
        await Database.OpenConnectionAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            UPDATE outbox_messages
            SET locked_until = now() + interval '5 minutes'
            WHERE id IN (
                SELECT id FROM outbox_messages
                WHERE processed_at IS NULL
                  AND attempts < {OutboxMessageTypes.MaxAttempts}
                  AND (locked_until IS NULL OR locked_until < now())
                ORDER BY created_at
                LIMIT @batchSize
                FOR UPDATE SKIP LOCKED
            )
            RETURNING id, type, payload, attempts
            """;

        var p = cmd.CreateParameter();
        p.ParameterName = "batchSize";
        p.Value = batchSize;
        cmd.Parameters.Add(p);

        var rows = new List<OutboxMessageBatchRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new OutboxMessageBatchRow(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3)));
        }

        return rows;
    }

    public Task MarkOutboxProcessedAsync(Guid id, CancellationToken cancellationToken = default) =>
        Database.ExecuteSqlAsync(
            $"""UPDATE outbox_messages SET processed_at = now(), error = NULL, locked_until = NULL WHERE id = {id}""",
            cancellationToken);

    public Task MarkOutboxFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        // Truncated here - single point - to match HasMaxLength(4000) in OutboxMessageConfiguration.
        var truncatedError = error.Length > 4000 ? error[..4000] : error;
        return Database.ExecuteSqlAsync(
            $"""UPDATE outbox_messages SET attempts = attempts + 1, error = {truncatedError}, locked_until = NULL WHERE id = {id}""",
            cancellationToken);
    }

    public Task<DashboardStatsRow> GetDashboardStatsRowAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        Database.SqlQuery<DashboardStatsRow>($"""
            SELECT
                (SELECT COUNT(*)::int
                 FROM storage_records
                 WHERE company_id = {companyId} AND storage_status = 'active')               AS active_storage_count,
                (SELECT COALESCE(SUM(monthly_rate), 0)::numeric
                 FROM storage_records
                 WHERE company_id = {companyId} AND storage_status = 'active')               AS total_monthly_rate,
                (SELECT COUNT(*)::int
                 FROM firearms
                 WHERE company_id = {companyId})                                             AS firearms_count,
                (SELECT COALESCE(SUM(CASE WHEN status IN ('sent','overdue') THEN total ELSE 0 END), 0)::numeric
                 FROM invoices
                 WHERE company_id = {companyId})                                             AS outstanding_amount,
                (SELECT COUNT(*)::int
                 FROM invoices
                 WHERE company_id = {companyId} AND status = 'overdue')                     AS overdue_count,
                (SELECT COUNT(*)::int
                 FROM firearm_licences
                 WHERE company_id = {companyId} AND status = 'renewal_due')                 AS licence_renewal_due,
                (SELECT COUNT(*)::int
                 FROM firearm_licences
                 WHERE company_id = {companyId} AND status = 'expired')                     AS licence_expired
            """)
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

                if (attempt < SerializableAttempts - 1)
                {
                    var delayMs = RetryBaseDelayMs * (attempt + 1) + Random.Shared.Next(RetryJitterMs);
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
        }

        return false;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("pg_trgm");
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