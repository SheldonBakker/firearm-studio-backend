using ErrorOr;
using FirearmStudio.Application.Products.DeleteProductImage;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class ProductImageDeleteTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private const string ImageKey = "companies/x/products/y/a.jpg";

    private static async Task<(Guid company, Guid productId)> SeedProductAsync(
        TestDatabaseFixture fixture, string? imageKey)
    {
        var company = Guid.NewGuid();
        await using var seed = fixture.CreateDbContext();
        await seed.Database.MigrateAsync();
        seed.Companies.Add(new Company { Id = company, Name = "C" });
        var product = new Product { CompanyId = company, Name = "P", Price = 10m, ImageKey = imageKey };
        seed.Products.Add(product);
        await seed.SaveChangesAsync();
        return (company, product.Id);
    }

    private async Task<string?> ReadImageKeyAsync(Guid company, Guid productId)
    {
        await using var db = fixture.CreateDbContext(company);
        return await db.Products.Where(p => p.Id == productId).Select(p => p.ImageKey).SingleAsync();
    }

    private static DeleteProductImageCommandHandler CreateHandler(
        ApplicationDbContext db, FakeFileStorage storage) =>
        new(db, storage, NullLogger<DeleteProductImageCommandHandler>.Instance);

    [Fact]
    public async Task Existing_image_is_cleared_and_deleted_from_storage()
    {
        var (company, productId) = await SeedProductAsync(fixture, ImageKey);
        var storage = new FakeFileStorage();

        await using (var db = fixture.CreateDbContext(company))
        {
            var result = await CreateHandler(db, storage).Handle(new DeleteProductImageCommand(productId), CancellationToken.None);

            Assert.Equal(Result.Deleted, result.Value);
        }

        Assert.Null(await ReadImageKeyAsync(company, productId));
        Assert.Equal([ImageKey], storage.Deleted);
    }

    [Fact]
    public async Task Product_without_image_returns_deleted_and_touches_no_storage()
    {
        var (company, productId) = await SeedProductAsync(fixture, null);
        var storage = new FakeFileStorage();

        await using var db = fixture.CreateDbContext(company);
        var result = await CreateHandler(db, storage).Handle(new DeleteProductImageCommand(productId), CancellationToken.None);

        Assert.Equal(Result.Deleted, result.Value);
        Assert.Empty(storage.Deleted);
    }

    [Fact]
    public async Task Storage_delete_failure_still_clears_the_database_key()
    {
        var (company, productId) = await SeedProductAsync(fixture, ImageKey);
        var storage = new FakeFileStorage { FailDeletes = true };

        await using (var db = fixture.CreateDbContext(company))
        {
            var result = await CreateHandler(db, storage).Handle(new DeleteProductImageCommand(productId), CancellationToken.None);

            Assert.Equal(Result.Deleted, result.Value);
        }

        Assert.Null(await ReadImageKeyAsync(company, productId));
        Assert.Empty(storage.Deleted);
    }

    [Fact]
    public async Task Unknown_product_returns_not_found()
    {
        var (company, _) = await SeedProductAsync(fixture, ImageKey);
        var storage = new FakeFileStorage();

        await using var db = fixture.CreateDbContext(company);
        var result = await CreateHandler(db, storage).Handle(new DeleteProductImageCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(DeleteProductImageCommandHandler.ErrorCodes.NotFound, result.FirstError.Code);
        Assert.Empty(storage.Deleted);
    }
}
