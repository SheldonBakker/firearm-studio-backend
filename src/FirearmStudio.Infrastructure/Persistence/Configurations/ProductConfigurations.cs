using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FirearmStudio.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Sku).HasMaxLength(64);
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.ImageKey).HasMaxLength(512);
        builder.Property(x => x.Price).HasPrecision(12, 2);
        builder.Property(x => x.CostPrice).HasPrecision(12, 2);
        builder.Property(x => x.StockQuantity).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.Property<string?>("SkuLower")
            .HasComputedColumnSql("lower(sku)", stored: true)
            .HasMaxLength(64);

        builder.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex("CompanyId", "SkuLower")
            .IsUnique()
            .HasFilter("sku IS NOT NULL");
        builder.HasIndex(x => new { x.CompanyId, x.Category });
        builder.HasIndex(x => new { x.CompanyId, x.IsActive });

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_products_price", "price >= 0");
            table.HasCheckConstraint("ck_products_cost_price", "cost_price is null or cost_price >= 0");
            table.HasCheckConstraint("ck_products_stock", "stock_quantity >= 0");
        });
    }
}
