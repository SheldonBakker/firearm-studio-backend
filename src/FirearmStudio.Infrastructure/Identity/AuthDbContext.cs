using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Infrastructure.Identity;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityUserContext<AppIdentityUser, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("identity");

        base.OnModelCreating(builder);

        builder.Entity<AppIdentityUser>().ToTable("users");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        builder.Entity<AppIdentityUser>(entity =>
        {
            entity.Property(u => u.Email).HasMaxLength(320).IsRequired();
            entity.Property(u => u.NormalizedEmail).HasMaxLength(320).IsRequired();
            entity.Property(u => u.UserName).HasMaxLength(320);
            entity.Property(u => u.NormalizedUserName).HasMaxLength(320);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(t => t.TokenHash).IsUnique();

            entity.HasIndex(t => t.UserId);

            entity.HasOne<AppIdentityUser>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OtpCode>(entity =>
        {
            entity.ToTable("otp_codes");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.CodeHash).HasMaxLength(128).IsRequired();

            entity.HasIndex(c => new { c.UserId, c.Purpose, c.CreatedAt });

            entity.HasOne<AppIdentityUser>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
