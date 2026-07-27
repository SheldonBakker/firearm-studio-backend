using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FirearmStudio.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(40);
        builder.Property(x => x.Subtotal).HasPrecision(12, 2);
        builder.Property(x => x.VatAmount).HasPrecision(12, 2);
        builder.Property(x => x.Total).HasPrecision(12, 2);
        builder.Property(x => x.DepositAmount).HasPrecision(12, 2);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_invoices_amounts",
                "subtotal >= 0 and vat_amount >= 0 and total = subtotal + vat_amount");
            table.HasCheckConstraint(
                "ck_invoices_kind",
                "kind between 0 and 1");
        });

        builder.HasIndex(x => new { x.CustomerId, x.InvoiceMonth });
        builder.HasIndex(x => new { x.CompanyId, x.Status });
        builder.HasIndex(x => new { x.CompanyId, x.InvoiceNumber }).IsUnique();
        builder.HasIndex(x => x.InvoiceNumber).HasMethod("gin").HasOperators("gin_trgm_ops");

        builder.HasIndex(x => new { x.CompanyId, x.CustomerId, x.InvoiceMonth })
            .IsUnique()
            .HasFilter("kind = 0");

        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne(i => i.Customer)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.Description).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Quantity).HasPrecision(12, 2);
        builder.Property(x => x.UnitPrice).HasPrecision(12, 2);
        builder.Property(x => x.LineTotal).HasPrecision(12, 2);

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_invoice_lines_amounts",
            "quantity > 0 and unit_price >= 0 and line_total >= 0"));

        builder.HasIndex(x => x.InvoiceId);
        builder.HasIndex(x => x.FirearmId);

        builder.HasOne(l => l.Invoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Firearm)
            .WithMany()
            .HasForeignKey(l => l.FirearmId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.Reference).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(4000);

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_payments_amount",
            "amount > 0"));

        builder.HasIndex(x => x.InvoiceId);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(80);
        builder.Property(x => x.OldValue).HasColumnType("jsonb");
        builder.Property(x => x.NewValue).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.AppUserId);
        builder.HasIndex(x => new { x.CompanyId, x.CreatedAt });

        builder.HasOne(x => x.AppUser)
            .WithMany()
            .HasForeignKey(x => x.AppUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
