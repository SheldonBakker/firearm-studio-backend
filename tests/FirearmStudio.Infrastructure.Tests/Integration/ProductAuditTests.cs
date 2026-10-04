using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class ProductAuditTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task Product_lifecycle_is_audited_without_cost_price_or_sku_lower()
    {
        var companyId = Guid.NewGuid();
        await using (var seed = fixture.CreateDbContext())
        {
            await seed.Database.MigrateAsync();
            seed.Companies.Add(new Company { Id = companyId, Name = "Audit" });
            await seed.SaveChangesAsync();
        }

        await using var db = fixture.CreateDbContext(companyId);
        var product = new Product { Name = "Audited", Sku = "AUD-1", Price = 10m, CostPrice = 42.50m };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        product.Price = 11m;
        await db.SaveChangesAsync();

        db.Products.Remove(product);
        await db.SaveChangesAsync();

        var logs = await db.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.CompanyId == companyId && a.EntityType == nameof(Product) && a.EntityId == product.Id)
            .ToListAsync();

        Assert.Contains(logs, a => a.Action == "Created");
        Assert.Contains(logs, a => a.Action == "Updated");
        Assert.Contains(logs, a => a.Action == "Deleted");

        foreach (var log in logs)
        {
            var payload = (log.OldValue ?? "") + (log.NewValue ?? "");
            Assert.DoesNotContain("CostPrice", payload);
            Assert.DoesNotContain("42.5", payload);
            Assert.DoesNotContain("SkuLower", payload);
        }
    }
}
