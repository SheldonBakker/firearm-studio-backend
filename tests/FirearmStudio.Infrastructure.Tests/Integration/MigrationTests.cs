using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class MigrationTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task Migrations_apply_to_an_empty_database()
    {
        await using var db = fixture.CreateDbContext();

        await db.Database.MigrateAsync();

        var applied = await db.Database.GetAppliedMigrationsAsync();

        Assert.NotEmpty(applied);
    }
}
