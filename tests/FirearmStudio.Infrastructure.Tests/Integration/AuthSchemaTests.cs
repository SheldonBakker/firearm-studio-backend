using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class AuthSchemaTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task Both_contexts_migrate_onto_the_same_database()
    {
        await fixture.MigrateAllAsync();

        await using var db = fixture.CreateDbContext();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            select table_name
            from information_schema.tables
            where table_schema = 'identity'
            order by table_name
            """;

        var tables = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        Assert.Contains("users", tables);
        Assert.Contains("refresh_tokens", tables);
        Assert.Contains("otp_codes", tables);

        Assert.DoesNotContain("roles", tables);
        Assert.DoesNotContain("user_roles", tables);
    }

    [Fact]
    public async Task Otp_purpose_enum_exists_exactly_once_in_public()
    {
        await fixture.MigrateAllAsync();

        await using var db = fixture.CreateDbContext();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            select n.nspname, count(e.enumlabel)
            from pg_type t
            join pg_namespace n on n.oid = t.typnamespace
            join pg_enum e on e.enumtypid = t.oid
            where t.typname = 'otp_purpose'
            group by n.nspname
            """;

        var rows = new List<(string Schema, long Labels)>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                rows.Add((reader.GetString(0), reader.GetInt64(1)));
            }
        }

        var row = Assert.Single(rows);
        Assert.Equal("public", row.Schema);
        Assert.Equal(3, row.Labels);
    }

    [Fact]
    public async Task Each_context_keeps_its_own_migrations_history()
    {
        await fixture.MigrateAllAsync();

        await using var db = fixture.CreateDbContext();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            select table_schema
            from information_schema.tables
            where table_name = '__EFMigrationsHistory'
            order by table_schema
            """;

        var schemas = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                schemas.Add(reader.GetString(0));
            }
        }

        Assert.Equal(["identity", "public"], schemas);
    }
}
