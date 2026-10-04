using FirearmStudio.Application.Products.GetProducts;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class ProductFilterSortTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private static readonly DateTime BaseTime = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static async Task<Guid> SeedAsync(TestDatabaseFixture fixture)
    {
        var company = Guid.NewGuid();

        await using var seed = fixture.CreateDbContext();
        await seed.Database.MigrateAsync();
        seed.Companies.Add(new Company { Id = company, Name = "C" });
        seed.Products.AddRange(
            new Product { CompanyId = company, Name = "Red Dot Sight", Sku = "OPT-1", Category = "optics", Price = 300m, StockQuantity = 4, IsActive = true },
            new Product { CompanyId = company, Name = "Rifle Case", Sku = "CASE-1", Category = "cases", Price = 120m, StockQuantity = 20, IsActive = true },
            new Product { CompanyId = company, Name = "Cleaning Kit", Sku = "CLN-1", Category = "maintenance", Price = 45m, StockQuantity = 0, IsActive = false });
        await seed.SaveChangesAsync();

        await SetCreatedAtAsync(seed, "OPT-1", BaseTime);
        await SetCreatedAtAsync(seed, "CASE-1", BaseTime.AddHours(1));
        await SetCreatedAtAsync(seed, "CLN-1", BaseTime.AddHours(2));
        return company;
    }

    private static async Task SetCreatedAtAsync(DbContext db, string sku, DateTime createdAt)
    {
        await db.Set<Product>().IgnoreQueryFilters()
            .Where(p => p.Sku == sku)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CreatedAt, createdAt));
    }

    private static GetProductsQuery Query(
        string? search = null, string? category = null, bool? isActive = null,
        decimal? minPrice = null, decimal? maxPrice = null, int? minStock = null, int? maxStock = null,
        string sortBy = "name", string sortDir = "asc") =>
        new(1, 20, search, category, isActive, minPrice, maxPrice, minStock, maxStock, sortBy, sortDir);

    [Fact]
    public async Task Search_matches_name_or_sku()
    {
        var company = await SeedAsync(fixture);
        await using var db = fixture.CreateDbContext(company);
        var handler = new GetProductsQueryHandler(db, new FakeFileStorage());

        var byName = await handler.Handle(Query(search: "rifle"), CancellationToken.None);
        var bySku = await handler.Handle(Query(search: "OPT-1"), CancellationToken.None);

        Assert.Equal(["Rifle Case"], byName.Value.Items.Select(p => p.Name));
        Assert.Equal(["Red Dot Sight"], bySku.Value.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Category_filter_is_exact_case_insensitive()
    {
        var company = await SeedAsync(fixture);
        await using var db = fixture.CreateDbContext(company);
        var handler = new GetProductsQueryHandler(db, new FakeFileStorage());

        var result = await handler.Handle(Query(category: "OPTICS"), CancellationToken.None);

        Assert.Equal(["Red Dot Sight"], result.Value.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Is_active_price_range_and_stock_range_filter()
    {
        var company = await SeedAsync(fixture);
        await using var db = fixture.CreateDbContext(company);
        var handler = new GetProductsQueryHandler(db, new FakeFileStorage());

        var active = await handler.Handle(Query(isActive: true), CancellationToken.None);
        var priced = await handler.Handle(Query(minPrice: 100m, maxPrice: 200m), CancellationToken.None);
        var stocked = await handler.Handle(Query(minStock: 1), CancellationToken.None);

        Assert.Equal(2, active.Value.Items.Count);
        Assert.Equal(["Rifle Case"], priced.Value.Items.Select(p => p.Name));
        Assert.Equal(2, stocked.Value.Items.Count);
    }

    [Theory]
    [InlineData("name", "asc", "Cleaning Kit,Red Dot Sight,Rifle Case")]
    [InlineData("name", "desc", "Rifle Case,Red Dot Sight,Cleaning Kit")]
    [InlineData("price", "asc", "Cleaning Kit,Rifle Case,Red Dot Sight")]
    [InlineData("price", "desc", "Red Dot Sight,Rifle Case,Cleaning Kit")]
    [InlineData("category", "asc", "Rifle Case,Cleaning Kit,Red Dot Sight")]
    [InlineData("category", "desc", "Red Dot Sight,Cleaning Kit,Rifle Case")]
    [InlineData("stockQuantity", "asc", "Cleaning Kit,Red Dot Sight,Rifle Case")]
    [InlineData("stockQuantity", "desc", "Rifle Case,Red Dot Sight,Cleaning Kit")]
    [InlineData("createdAt", "asc", "Red Dot Sight,Rifle Case,Cleaning Kit")]
    [InlineData("createdAt", "desc", "Cleaning Kit,Rifle Case,Red Dot Sight")]
    public async Task Sort_orders_rows_for_every_field_and_direction(string sortBy, string sortDir, string expected)
    {
        var company = await SeedAsync(fixture);
        await using var db = fixture.CreateDbContext(company);
        var handler = new GetProductsQueryHandler(db, new FakeFileStorage());

        var result = await handler.Handle(Query(sortBy: sortBy, sortDir: sortDir), CancellationToken.None);

        Assert.Equal(expected.Split(','), result.Value.Items.Select(p => p.Name));
    }

    [Theory]
    [InlineData("name", "asc")]
    [InlineData("name", "desc")]
    [InlineData("price", "asc")]
    [InlineData("price", "desc")]
    [InlineData("category", "asc")]
    [InlineData("category", "desc")]
    [InlineData("stockQuantity", "asc")]
    [InlineData("stockQuantity", "desc")]
    [InlineData("createdAt", "asc")]
    [InlineData("createdAt", "desc")]
    public async Task Equal_sort_keys_fall_back_to_id_ascending(string sortBy, string sortDir)
    {
        var company = Guid.NewGuid();
        var prefix = Guid.NewGuid().ToString("N")[..30];
        var smallerId = Guid.Parse(prefix + "01");
        var largerId = Guid.Parse(prefix + "02");

        await using (var seed = fixture.CreateDbContext())
        {
            await seed.Database.MigrateAsync();
            seed.Companies.Add(new Company { Id = company, Name = "C" });
            seed.Products.Add(new Product { Id = largerId, CompanyId = company, Name = "Twin", Sku = "TWIN-B", Category = "same", Price = 10m, StockQuantity = 5 });
            await seed.SaveChangesAsync();
            seed.Products.Add(new Product { Id = smallerId, CompanyId = company, Name = "Twin", Sku = "TWIN-A", Category = "same", Price = 10m, StockQuantity = 5 });
            await seed.SaveChangesAsync();

            await SetCreatedAtAsync(seed, "TWIN-A", BaseTime);
            await SetCreatedAtAsync(seed, "TWIN-B", BaseTime);
        }

        await using var db = fixture.CreateDbContext(company);
        var handler = new GetProductsQueryHandler(db, new FakeFileStorage());

        var result = await handler.Handle(Query(sortBy: sortBy, sortDir: sortDir), CancellationToken.None);

        Assert.Equal([smallerId, largerId], result.Value.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task Image_url_is_null_when_no_image_is_stored()
    {
        var company = await SeedAsync(fixture);
        await using var db = fixture.CreateDbContext(company);
        var handler = new GetProductsQueryHandler(db, new FakeFileStorage());

        var result = await handler.Handle(Query(), CancellationToken.None);

        Assert.All(result.Value.Items, item => Assert.Null(item.ImageUrl));
    }
}
