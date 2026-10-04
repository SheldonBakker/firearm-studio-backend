using FirearmStudio.Application.Model;
using FirearmStudio.Application.Products;
using FirearmStudio.Application.Products.UpdateProduct;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class ProductSkuUniquenessTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private static async Task<(Guid a, Guid b)> SeedCompaniesAsync(TestDatabaseFixture fixture)
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await using var seed = fixture.CreateDbContext();
        await seed.Database.MigrateAsync();
        seed.Companies.AddRange(new Company { Id = a, Name = "A" }, new Company { Id = b, Name = "B" });
        await seed.SaveChangesAsync();
        return (a, b);
    }

    [Fact]
    public async Task Same_sku_in_one_company_violates_the_unique_index()
    {
        var (a, _) = await SeedCompaniesAsync(fixture);

        await using var first = fixture.CreateDbContext(a);
        first.Products.Add(new Product { Name = "One", Sku = "ABC", Price = 1m });
        await first.SaveChangesAsync();

        await using var second = fixture.CreateDbContext(a);
        second.Products.Add(new Product { Name = "Two", Sku = "ABC", Price = 1m });
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Same_sku_differing_only_in_case_violates_the_unique_index()
    {
        var (a, _) = await SeedCompaniesAsync(fixture);

        await using var first = fixture.CreateDbContext(a);
        first.Products.Add(new Product { Name = "One", Sku = "ABC", Price = 1m });
        await first.SaveChangesAsync();

        await using var second = fixture.CreateDbContext(a);
        second.Products.Add(new Product { Name = "Two", Sku = "abc", Price = 1m });
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Null_skus_coexist_and_same_sku_in_two_companies_coexists()
    {
        var (a, b) = await SeedCompaniesAsync(fixture);

        await using (var db = fixture.CreateDbContext(a))
        {
            db.Products.AddRange(
                new Product { Name = "NullOne", Sku = null, Price = 1m },
                new Product { Name = "NullTwo", Sku = null, Price = 1m });
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateDbContext(a))
        {
            db.Products.Add(new Product { Name = "SharedA", Sku = "SHARED", Price = 1m });
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateDbContext(b))
        {
            db.Products.Add(new Product { Name = "SharedB", Sku = "SHARED", Price = 1m });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Update_keeping_own_sku_succeeds_but_colliding_with_another_conflicts()
    {
        var (a, _) = await SeedCompaniesAsync(fixture);
        var storage = new FakeFileStorage();

        Guid keptId;
        await using (var db = fixture.CreateDbContext(a))
        {
            var kept = new Product { Name = "Kept", Sku = "KEEP", Price = 1m };
            var other = new Product { Name = "Other", Sku = "OTHER", Price = 1m };
            db.Products.AddRange(kept, other);
            await db.SaveChangesAsync();
            keptId = kept.Id;
        }

        await using (var db = fixture.CreateDbContext(a))
        {
            var handler = new UpdateProductCommandHandler(db, storage);
            var request = new UpdateProductRequest(
                default, default, new Optional<string?>("KEEP"), new Optional<decimal>(2m),
                default, default, default, default);
            var result = await handler.Handle(new UpdateProductCommand(keptId, request), CancellationToken.None);
            Assert.False(result.IsError);
        }

        await using (var db = fixture.CreateDbContext(a))
        {
            var handler = new UpdateProductCommandHandler(db, storage);
            var request = new UpdateProductRequest(
                default, default, new Optional<string?>("OTHER"), default, default, default, default, default);
            var result = await handler.Handle(new UpdateProductCommand(keptId, request), CancellationToken.None);
            Assert.True(result.IsError);
            Assert.Equal(UpdateProductCommandHandler.ErrorCodes.SkuConflict, result.FirstError.Code);
        }
    }
}
