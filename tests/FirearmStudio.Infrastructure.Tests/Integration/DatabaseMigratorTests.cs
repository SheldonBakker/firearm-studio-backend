using FirearmStudio.WebApi.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class DatabaseMigratorTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task Migrates_both_contexts_on_a_fresh_database()
    {
        await using var app = fixture.CreateDbContext();
        await using var auth = fixture.CreateAuthDbContext();

        await DatabaseMigrator.MigrateAsync(app, NullLogger.Instance, CancellationToken.None);
        await DatabaseMigrator.MigrateAsync(auth, NullLogger.Instance, CancellationToken.None);

        Assert.Empty(await app.Database.GetPendingMigrationsAsync());
        Assert.NotEmpty(await app.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await auth.Database.GetPendingMigrationsAsync());
        Assert.NotEmpty(await auth.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task Running_twice_is_a_no_op()
    {
        await using var app = fixture.CreateDbContext();
        await using var auth = fixture.CreateAuthDbContext();

        await DatabaseMigrator.MigrateAsync(app, NullLogger.Instance, CancellationToken.None);
        await DatabaseMigrator.MigrateAsync(auth, NullLogger.Instance, CancellationToken.None);
        await DatabaseMigrator.MigrateAsync(app, NullLogger.Instance, CancellationToken.None);
        await DatabaseMigrator.MigrateAsync(auth, NullLogger.Instance, CancellationToken.None);

        Assert.Empty(await app.Database.GetPendingMigrationsAsync());
        Assert.Empty(await auth.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public void Switch_is_detected_only_when_present()
    {
        Assert.True(DatabaseMigrator.IsRequested(["--migrate"]));
        Assert.False(DatabaseMigrator.IsRequested([]));
        Assert.False(DatabaseMigrator.IsRequested(["--other"]));
    }
}
