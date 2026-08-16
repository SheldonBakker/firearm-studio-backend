using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FirearmStudio.Infrastructure.Persistence.Configurations;

internal sealed class AccountingConnectionConfiguration : IEntityTypeConfiguration<AccountingConnection>
{
    public void Configure(EntityTypeBuilder<AccountingConnection> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.ApiKeyCiphertext).IsRequired();
        builder.Property(x => x.UsernameCiphertext).IsRequired();
        builder.Property(x => x.PasswordCiphertext).IsRequired();
        builder.Property(x => x.ExternalCompanyName).IsRequired().HasMaxLength(200);

        builder.HasIndex(x => x.CompanyId).IsUnique();

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_accounting_connections_external_company_id",
            "external_company_id > 0"));
    }
}
