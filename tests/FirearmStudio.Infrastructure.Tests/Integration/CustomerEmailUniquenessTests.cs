using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class CustomerEmailUniquenessTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task Unique_index_on_company_and_lowered_email_exists()
    {
        await using var db = fixture.CreateDbContext();
        await db.Database.MigrateAsync();

        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            select indexdef
            from pg_indexes
            where tablename = 'customers'
              and indexname = 'ix_customers_company_id_lower_email'
            """;

        var definition = (string?)await command.ExecuteScalarAsync();

        Assert.NotNull(definition);
        Assert.Contains("UNIQUE", definition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lower(", definition, StringComparison.OrdinalIgnoreCase);
    }
}
