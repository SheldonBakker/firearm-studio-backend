using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FirearmStudio.Infrastructure.Persistence.Configurations;

internal sealed class LicenceReminderConfiguration : IEntityTypeConfiguration<LicenceReminder>
{
    public void Configure(EntityTypeBuilder<LicenceReminder> builder)
    {
        builder.ConfigureTenant();

        builder.HasIndex(x => new { x.LicenceId, x.Tier }).IsUnique();

        builder.HasOne(x => x.Licence)
            .WithMany()
            .HasForeignKey(x => x.LicenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
