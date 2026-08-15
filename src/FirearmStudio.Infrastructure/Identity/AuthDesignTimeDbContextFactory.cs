using FirearmStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FirearmStudio.Infrastructure.Identity;

public sealed class AuthDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<AuthDbContext>
{
    private const string ConnectionStringKey =
        "ConnectionStrings__DefaultConnection";

    public AuthDbContext CreateDbContext(string[] args)
    {
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

        var dataSource = NpgsqlDataSourceFactory.Build(connectionString);

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(dataSource, npgsql =>
            {
                NpgsqlDataSourceFactory.MapAuthEnums(npgsql);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
            })
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AuthDbContext(options);
    }
}
