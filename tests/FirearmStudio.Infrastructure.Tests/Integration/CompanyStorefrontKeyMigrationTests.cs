using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class CompanyStorefrontKeyMigrationTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task Companies_storefront_key_column_is_nullable_varchar_64()
    {
        await fixture.MigrateAllAsync();

        await using var db = fixture.CreateDbContext();
        await using var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT is_nullable, character_maximum_length
            FROM information_schema.columns
            WHERE table_name = 'companies'
              AND column_name = 'storefront_key'
            """;
        await using var reader = await cmd.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync(), "storefront_key column not found in companies table");
        Assert.Equal("YES", reader.GetString(0));
        Assert.Equal(64, reader.GetInt32(1));
    }

    [Fact]
    public async Task Unique_index_ix_companies_storefront_key_exists_and_is_unique()
    {
        await fixture.MigrateAllAsync();

        await using var db = fixture.CreateDbContext();
        await using var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT i.indisunique
            FROM pg_indexes pi2
            JOIN pg_class c ON c.relname = pi2.tablename
            JOIN pg_index i ON i.indrelid = c.oid
            JOIN pg_class ic ON ic.oid = i.indexrelid AND ic.relname = pi2.indexname
            WHERE pi2.tablename = 'companies'
              AND pi2.indexname = 'ix_companies_storefront_key'
            """;
        await using var reader = await cmd.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync(), "ix_companies_storefront_key index not found");
        Assert.True(reader.GetBoolean(0), "ix_companies_storefront_key is not unique");
    }

    [Fact]
    public async Task Two_companies_with_null_storefront_key_save_successfully()
    {
        await fixture.MigrateAllAsync();

        await using var db = fixture.CreateDbContext();

        db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Company A" });
        db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Company B" });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Two_companies_with_the_same_storefront_key_throw_DbUpdateException()
    {
        await fixture.MigrateAllAsync();

        const string sharedKey = "fs_pk_duplicatekey";

        await using var first = fixture.CreateDbContext();
        first.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "First", StorefrontKey = sharedKey });
        await first.SaveChangesAsync();

        await using var second = fixture.CreateDbContext();
        second.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Second", StorefrontKey = sharedKey });

        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }
}
