using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class ProductTenantIsolationTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task Query_filter_hides_another_companys_products()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();

        await using (var seed = fixture.CreateDbContext())
        {
            await seed.Database.MigrateAsync();

            seed.Companies.AddRange(
                new Company { Id = companyA, Name = "Company A" },
                new Company { Id = companyB, Name = "Company B" });

            seed.Products.AddRange(
                new Product { CompanyId = companyA, Name = "A Product", Price = 1m },
                new Product { CompanyId = companyB, Name = "B Product", Price = 1m });

            await seed.SaveChangesAsync();
        }

        await using var scoped = fixture.CreateDbContext(companyA);

        var names = await scoped.Products
            .Select(p => p.Name)
            .ToListAsync();

        Assert.Equal(["A Product"], names);
    }
}
