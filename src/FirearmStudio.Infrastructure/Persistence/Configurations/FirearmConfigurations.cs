using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FirearmStudio.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ConfigureTenant();
        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.CompanyName).HasMaxLength(200);
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
    }
}

internal sealed class FirearmConfiguration : IEntityTypeConfiguration<Firearm>
{
    public void Configure(EntityTypeBuilder<Firearm> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.Make).IsRequired().HasMaxLength(120);
        builder.Property(x => x.SerialNumber).IsRequired().HasMaxLength(120);

        builder.HasIndex(x => x.SerialNumber);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.CompanyId, x.SerialNumber }).IsUnique();

        builder.HasOne(f => f.Customer)
            .WithMany(c => c.Firearms)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class FirearmLicenceConfiguration : IEntityTypeConfiguration<FirearmLicence>
{
    public void Configure(EntityTypeBuilder<FirearmLicence> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.LicenceNumber).IsRequired().HasMaxLength(120);

        builder.Property(x => x.RenewalDueOn)
            .HasComputedColumnSql("expires_on - 90", stored: true)
            .ValueGeneratedOnAddOrUpdate();
        builder.Property(x => x.RenewalDueOn).Metadata
            .SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        builder.HasIndex(x => x.FirearmId);
        builder.HasIndex(x => x.ExpiresOn);
        builder.HasIndex(x => x.RenewalDueOn);
        builder.HasIndex(x => new { x.FirearmId, x.LicenceNumber }).IsUnique();

        builder.HasOne(l => l.Firearm)
            .WithMany(f => f.Licences)
            .HasForeignKey(l => l.FirearmId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class StorageRecordConfiguration : IEntityTypeConfiguration<StorageRecord>
{
    public void Configure(EntityTypeBuilder<StorageRecord> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.MonthlyRate).HasPrecision(12, 2);
        builder.Property(x => x.StorageLocation).HasMaxLength(200);
        builder.Property(x => x.RackNumber).HasMaxLength(60);
        builder.Property(x => x.SafeNumber).HasMaxLength(60);

        builder.HasIndex(x => x.FirearmId);
        builder.HasIndex(x => x.FirearmId)
            .HasFilter("storage_status = 'active'")
            .HasDatabaseName("ix_storage_records_active");

        builder.HasOne(s => s.Firearm)
            .WithMany(f => f.StorageRecords)
            .HasForeignKey(s => s.FirearmId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
