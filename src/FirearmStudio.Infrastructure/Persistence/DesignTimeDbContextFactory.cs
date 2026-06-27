using FirearmStudio.Application.Model.Options;
using FirearmStudio.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FirearmStudio.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("firearm-studio-backend")
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration[$"{DatabaseSettings.SectionName}:ConnectionString"]
            ?? throw new InvalidOperationException(
                "No connection string found. Set DatabaseSettings:ConnectionString via user-secrets " +
                "or the DatabaseSettings__ConnectionString environment variable.");

        var dataSource = SupabaseDataSourceFactory.Build(connectionString);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(dataSource, SupabaseDataSourceFactory.MapEnums)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ApplicationDbContext(options, new NullTenantContext());
    }
}
