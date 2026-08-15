using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
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
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.AddressLine1).HasMaxLength(200);
        builder.Property(x => x.AddressLine2).HasMaxLength(200);
        builder.Property(x => x.City).HasMaxLength(120);
        builder.Property(x => x.Province).HasMaxLength(120);
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.BankName).HasMaxLength(200);
        builder.Property(x => x.BankAccountHolder).HasMaxLength(200);
        builder.Property(x => x.BankAccountNumber).HasMaxLength(34);
        builder.Property(x => x.BankBranchCode).HasMaxLength(20);
        builder.Property(x => x.BankAccountType).HasMaxLength(20);
        builder.Property(x => x.BankSwiftCode).HasMaxLength(11);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.DueDays).HasDefaultValue(30);
        builder.Property(x => x.AutoBillingEnabled).HasDefaultValue(true);
        builder.Property(x => x.DepositMode).HasDefaultValue(DepositMode.None);
        builder.Property(x => x.DepositValue).HasPrecision(12, 2).HasDefaultValue(0m);
        builder.Property(x => x.DepositWindowHours).HasDefaultValue(48);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_companies_due_days",
                "due_days between 0 and 365");
            table.HasCheckConstraint(
                "ck_companies_deposit_value",
                "deposit_value >= 0");
            table.HasCheckConstraint(
                "ck_companies_deposit_percentage",
                "deposit_mode <> 'percentage' or deposit_value <= 100");
            table.HasCheckConstraint(
                "ck_companies_deposit_window_hours",
                "deposit_window_hours between 1 and 336");
        });
    }
}

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_app_users_role",
            "role between 0 and 3"));

        builder.HasIndex(x => x.CompanyId);

        builder.HasIndex(x => x.AuthUserId).IsUnique();

        builder.HasIndex(x => new { x.CompanyId, x.Email }).IsUnique();

        builder.HasIndex(x => x.FullName).HasMethod("gin").HasOperators("gin_trgm_ops");

        builder.HasOne(u => u.Company)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
