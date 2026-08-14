using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class TenantIsolationTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task Query_filter_hides_another_companys_customers()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();

        await using (var seed = fixture.CreateDbContext())
        {
            await seed.Database.MigrateAsync();

            seed.Companies.AddRange(
                new Company { Id = companyA, Name = "Company A" },
                new Company { Id = companyB, Name = "Company B" });

            seed.Customers.AddRange(
                new Customer { CompanyId = companyA, FullName = "Alice", Email = "alice@example.com" },
                new Customer { CompanyId = companyB, FullName = "Bob", Email = "bob@example.com" });

            await seed.SaveChangesAsync();
        }

        await using var scoped = fixture.CreateDbContext(companyA);

        var names = await scoped.Customers
            .Select(c => c.FullName)
            .ToListAsync();

        Assert.Equal(["Alice"], names);
    }
}
