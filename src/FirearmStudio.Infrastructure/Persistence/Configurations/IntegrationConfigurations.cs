using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FirearmStudio.Infrastructure.Persistence.Configurations;

internal sealed class SageConnectionConfiguration : IEntityTypeConfiguration<SageConnection>
{
    public void Configure(EntityTypeBuilder<SageConnection> builder)
    {
        builder.ConfigureTenant();

        builder.Property(x => x.ApiKeyCiphertext).IsRequired();
        builder.Property(x => x.UsernameCiphertext).IsRequired();
        builder.Property(x => x.PasswordCiphertext).IsRequired();
        builder.Property(x => x.SageCompanyName).IsRequired().HasMaxLength(200);

        builder.HasIndex(x => x.CompanyId).IsUnique();

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_sage_connections_sage_company_id",
            "sage_company_id > 0"));
    }
}
