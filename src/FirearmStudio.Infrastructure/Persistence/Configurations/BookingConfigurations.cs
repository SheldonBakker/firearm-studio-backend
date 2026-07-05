using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FirearmStudio.Infrastructure.Persistence.Configurations;

internal sealed class ShootingRangeConfiguration : IEntityTypeConfiguration<ShootingRange>
{
    public void Configure(EntityTypeBuilder<ShootingRange> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.SlotIntervalMinutes).HasDefaultValue(30);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_shooting_ranges_lane_count",
                "lane_count between 1 and 100");
            table.HasCheckConstraint(
                "ck_shooting_ranges_slot_interval",
                "slot_interval_minutes between 5 and 240");
        });
    }
}

internal sealed class RangeOperatingHoursConfiguration : IEntityTypeConfiguration<RangeOperatingHours>
{
    public void Configure(EntityTypeBuilder<RangeOperatingHours> builder)
    {
        builder.ConfigureTenant();

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_range_operating_hours_day",
                "day between 0 and 6");
            table.HasCheckConstraint(
                "ck_range_operating_hours_window",
                "close_time > open_time");
        });

        builder.HasIndex(x => new { x.ShootingRangeId, x.Day }).IsUnique();

        builder.HasOne(h => h.ShootingRange)
            .WithMany(r => r.OperatingHours)
            .HasForeignKey(h => h.ShootingRangeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Price).HasPrecision(12, 2);
        builder.Property(x => x.MaxShooters).HasDefaultValue(1);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_packages_price",
                "price >= 0");
            table.HasCheckConstraint(
                "ck_packages_duration",
                "duration_minutes between 15 and 480");
            table.HasCheckConstraint(
                "ck_packages_max_shooters",
                "max_shooters between 1 and 20");
        });
    }
}

internal sealed class PackageItemConfiguration : IEntityTypeConfiguration<PackageItem>
{
    public void Configure(EntityTypeBuilder<PackageItem> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.Description).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Quantity).HasPrecision(12, 2);

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_package_items_quantity",
            "quantity > 0"));

        builder.HasIndex(x => x.PackageId);

        builder.HasOne(i => i.Package)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.BookingNumber).IsRequired().HasMaxLength(40);
        builder.Property(x => x.PackageName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PackagePrice).HasPrecision(12, 2);
        builder.Property(x => x.ShooterCount).HasDefaultValue(1);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_bookings_times",
                "end_time > start_time");
            table.HasCheckConstraint(
                "ck_bookings_price",
                "package_price >= 0");
            table.HasCheckConstraint(
                "ck_bookings_shooters",
                "shooter_count between 1 and 20");
            table.HasCheckConstraint(
                "ck_bookings_status",
                "status between 0 and 4");
        });

        // Drives every availability/overlap query.
        builder.HasIndex(x => new { x.CompanyId, x.ShootingRangeId, x.BookingDate });
        builder.HasIndex(x => new { x.CompanyId, x.Status });
        builder.HasIndex(x => new { x.CompanyId, x.BookingNumber }).IsUnique();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.PackageId);
        builder.HasIndex(x => x.InvoiceId);

        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne(b => b.ShootingRange)
            .WithMany(r => r.Bookings)
            .HasForeignKey(b => b.ShootingRangeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Package)
            .WithMany(p => p.Bookings)
            .HasForeignKey(b => b.PackageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Customer)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Invoice)
            .WithMany()
            .HasForeignKey(b => b.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
