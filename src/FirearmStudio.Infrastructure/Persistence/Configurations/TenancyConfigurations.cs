using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FirearmStudio.Infrastructure.Persistence.Configurations;

internal static class TenantEntityConfig
{
    public static void ConfigureTenant<T>(this EntityTypeBuilder<T> builder) where T : class, ITenantEntity
    {
        builder.HasIndex(e => e.CompanyId);
        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.RegistrationNumber).HasMaxLength(50);
        builder.Property(x => x.VatNumber).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
    }
}

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasIndex(x => x.CompanyId);

        builder.HasIndex(x => x.AuthUserId).IsUnique();

        builder.HasIndex(x => new { x.CompanyId, x.Email }).IsUnique();

        builder.HasOne(u => u.Company)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
