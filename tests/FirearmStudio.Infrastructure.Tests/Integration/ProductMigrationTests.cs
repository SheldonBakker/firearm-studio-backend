using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class ProductMigrationTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task Migration_applies_to_an_empty_database()
    {
        await using var db = fixture.CreateDbContext();

        await db.Database.MigrateAsync();

        var applied = await db.Database.GetAppliedMigrationsAsync();

        Assert.NotEmpty(applied);
        Assert.Contains(applied, name => name.EndsWith("AddProducts", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Check_constraints_reject_negative_price_cost_and_stock()
    {
        var company = Guid.NewGuid();

        await using (var seed = fixture.CreateDbContext())
        {
            await seed.Database.MigrateAsync();
            seed.Companies.Add(new Company { Id = company, Name = "C" });
            await seed.SaveChangesAsync();
        }

        await AssertSaveFailsAsync(new Product { Name = "neg price", Price = -1m });
        await AssertSaveFailsAsync(new Product { Name = "neg cost", Price = 1m, CostPrice = -1m });
        await AssertSaveFailsAsync(new Product { Name = "neg stock", Price = 1m, StockQuantity = -1 });

        async Task AssertSaveFailsAsync(Product product)
        {
            await using var db = fixture.CreateDbContext(company);
            db.Products.Add(product);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }
}
