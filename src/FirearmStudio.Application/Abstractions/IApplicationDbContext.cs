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
    DbSet<StorageRecord> StorageRecords { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLine> InvoiceLines { get; }
    DbSet<Payment> Payments { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
