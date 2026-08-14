using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Infrastructure.Identity;
using FirearmStudio.Infrastructure.Persistence;
using FirearmStudio.Infrastructure.Persistence.Interceptors;
using FirearmStudio.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class TestDatabaseFixture : IAsyncLifetime
{
    private const string ThrowawayPrefix = "firearmstudio_test_";

    private const string AdminConnectionKey = "TestDatabase__AdminConnection";

    private readonly string _databaseName = $"{ThrowawayPrefix}{Guid.NewGuid():N}";

    private string _adminConnectionString = string.Empty;

    private NpgsqlDataSource? _dataSource;

    public string ConnectionString { get; private set; } = string.Empty;

    private NpgsqlDataSource DataSource =>
        _dataSource ?? throw new InvalidOperationException("Fixture is not initialised.");

    public async Task InitializeAsync()
    {
        DotNetEnv.Env.NoClobber().TraversePath().Load();

        _adminConnectionString =
            Environment.GetEnvironmentVariable(AdminConnectionKey)
            ?? throw new InvalidOperationException(
                $"'{AdminConnectionKey}' is not set. Integration tests need a superuser " +
                "connection to create and drop throwaway databases. See .env.example.");

        GuardThrowawayName(_databaseName);

        await using (var admin = new NpgsqlConnection(_adminConnectionString))
        {
            await admin.OpenAsync();

            await using var command = admin.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = _databaseName,
            MaxPoolSize = 5,
        }.ConnectionString;

        _dataSource = NpgsqlDataSourceFactory.Build(ConnectionString);
    }

    public ApplicationDbContext CreateDbContext() =>
        CreateDbContext(new NullTenantContext());

    public ApplicationDbContext CreateDbContext(Guid companyId) =>
        CreateDbContext(new BypassTenantContext { CompanyId = companyId });

    public AuthDbContext CreateAuthDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(DataSource, npgsql =>
            {
                NpgsqlDataSourceFactory.MapAuthEnums(npgsql);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
            })
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AuthDbContext(options);
    }

    public async Task MigrateAllAsync()
    {
        await using (var app = CreateDbContext())
        {
            await app.Database.MigrateAsync();
        }

        await using var auth = CreateAuthDbContext();
        await auth.Database.MigrateAsync();
    }

    public ApplicationDbContext CreateDbContext(ITenantContext tenant)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DataSource, NpgsqlDataSourceFactory.MapEnums)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new TenantAndAuditInterceptor(tenant, new AnonymousCurrentUserService()))
            .Options;

        return new ApplicationDbContext(options, tenant);
    }

    private sealed class AnonymousCurrentUserService : ICurrentUserService
    {
        public CurrentUser User => CurrentUser.Anonymous;
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(_adminConnectionString))
        {
            return;
        }

        GuardThrowawayName(_databaseName);

        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
            _dataSource = null;
        }

        await using var admin = new NpgsqlConnection(_adminConnectionString);
        await admin.OpenAsync();

        await using var command = admin.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }

    private static void GuardThrowawayName(string databaseName)
    {
        if (!databaseName.StartsWith(ThrowawayPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to operate on '{databaseName}'. This fixture may only create " +
                $"and drop databases named '{ThrowawayPrefix}*'.");
        }
    }

}
