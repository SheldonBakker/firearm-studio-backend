using FirearmStudio.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FirearmStudio.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string ConnectionStringKey =
        "ConnectionStrings__DefaultConnection";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // NoClobber: an explicitly-set environment variable (e.g. a connection string override
        // passed inline) must win over .env - .env should only fill in variables that are unset.
        DotNetEnv.Env
            .NoClobber()
            .TraversePath()
            .Load();

        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionStringKey);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"The '{ConnectionStringKey}' variable is missing or empty in the .env file.");
        }

        var dataSource =
            SupabaseDataSourceFactory.Build(connectionString);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                dataSource,
                SupabaseDataSourceFactory.MapEnums)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ApplicationDbContext(
            options,
            new NullTenantContext());
    }
}
