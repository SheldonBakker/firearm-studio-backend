using FirearmStudio.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FirearmStudio.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        if (File.Exists(".env"))
        {
            DotNetEnv.Env.Load();
        }

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("firearm-studio-backend")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No connection string found. Set ConnectionStrings:DefaultConnection via the .env " +
                "(ConnectionStrings__DefaultConnection), user-secrets, or an environment variable.");

        var dataSource = SupabaseDataSourceFactory.Build(connectionString);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(dataSource, SupabaseDataSourceFactory.MapEnums)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ApplicationDbContext(options, new NullTenantContext());
    }
}
