using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Products.GetPublicProducts;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class PublicProductsQueryTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private const string ImageKey = "companies/a/products/x/i.jpg";

    private sealed record SeedResult(
        Guid CompanyA,
        Guid CompanyB,
        Guid CompanyC,
        Guid CompanyD,
        string KeyA,
        string KeyB,
        string KeyC);

    private static async Task<SeedResult> SeedAsync(TestDatabaseFixture fix)
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var companyC = Guid.NewGuid();
        var companyD = Guid.NewGuid();
        var keyA = StorefrontKey.Generate();
        var keyB = StorefrontKey.Generate();
        var keyC = StorefrontKey.Generate();

        await using var seed = fix.CreateDbContext();
        await seed.Database.MigrateAsync();

        seed.Companies.AddRange(
            new Company { Id = companyA, Name = $"Company A {companyA:N}", IsActive = true, StorefrontKey = keyA },
            new Company { Id = companyB, Name = $"Company B {companyB:N}", IsActive = true, StorefrontKey = keyB },
            new Company { Id = companyC, Name = $"Company C {companyC:N}", IsActive = false, StorefrontKey = keyC },
            new Company { Id = companyD, Name = $"Company D {companyD:N}", IsActive = true, StorefrontKey = null });

        seed.Products.AddRange(
            new Product { CompanyId = companyA, Name = "Ammo Box", Category = "Ammo", StockQuantity = 5, IsActive = true, ImageKey = ImageKey, Price = 10m },
            new Product { CompanyId = companyA, Name = "Zip Case", Category = "Cases", StockQuantity = 0, IsActive = true, ImageKey = null, Price = 20m },
            new Product { CompanyId = companyA, Name = "Hidden", Category = "Ammo", StockQuantity = 1, IsActive = false, Price = 5m },
            new Product { CompanyId = companyB, Name = "B Product", Category = "Other", StockQuantity = 3, IsActive = true, Price = 15m });

        await seed.SaveChangesAsync();
        return new SeedResult(companyA, companyB, companyC, companyD, keyA, keyB, keyC);
    }

    private static TenantContext CreateTenantContext() =>
        new(new AnonymousCurrentUserService());

    private GetPublicProductsQueryHandler CreateHandler(TenantContext tenant) =>
        new(fixture.CreateDbContext(tenant), tenant, new FakeFileStorage());

    private static GetPublicProductsQuery Query(
        Guid companyId,
        string? key,
        string? search = null,
        string? category = null,
        string sortBy = "name",
        string sortDir = "asc",
        int pageNumber = 1,
        int pageSize = 20) =>
        new(companyId, key, pageNumber, pageSize, search, category, sortBy, sortDir);

    [Fact]
    public async Task Correct_key_returns_only_active_products_for_company_ordered_by_name()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(seed.CompanyA, seed.KeyA), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(["Ammo Box", "Zip Case"], result.Value.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Ammo_box_has_in_stock_true_and_presigned_image_url()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(seed.CompanyA, seed.KeyA), CancellationToken.None);

        Assert.False(result.IsError);
        var ammoBox = result.Value.Items.Single(p => p.Name == "Ammo Box");
        Assert.True(ammoBox.InStock);
        Assert.Equal($"https://files.test/{ImageKey}", ammoBox.ImageUrl);
    }

    [Fact]
    public async Task Zip_case_has_in_stock_false_and_null_image_url()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(seed.CompanyA, seed.KeyA), CancellationToken.None);

        Assert.False(result.IsError);
        var zipCase = result.Value.Items.Single(p => p.Name == "Zip Case");
        Assert.False(zipCase.InStock);
        Assert.Null(zipCase.ImageUrl);
    }

    [Fact]
    public async Task Correct_key_never_returns_other_company_products()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(seed.CompanyA, seed.KeyA), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.DoesNotContain(result.Value.Items, p => p.Name == "B Product");
    }

    [Fact]
    public async Task Wrong_key_returns_not_found()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(seed.CompanyA, seed.KeyB), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(GetPublicProductsQueryHandler.ErrorCodes.NotFound, result.FirstError.Code);
    }

    [Fact]
    public async Task Null_key_returns_not_found()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(seed.CompanyA, null), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(GetPublicProductsQueryHandler.ErrorCodes.NotFound, result.FirstError.Code);
    }

    [Fact]
    public async Task Empty_key_returns_not_found()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(seed.CompanyA, ""), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(GetPublicProductsQueryHandler.ErrorCodes.NotFound, result.FirstError.Code);
    }

    [Fact]
    public async Task Company_with_null_stored_key_returns_not_found_for_any_key()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(seed.CompanyD, seed.KeyA), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(GetPublicProductsQueryHandler.ErrorCodes.NotFound, result.FirstError.Code);
    }

    [Fact]
    public async Task Inactive_company_with_correct_key_returns_not_found()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(seed.CompanyC, seed.KeyC), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(GetPublicProductsQueryHandler.ErrorCodes.NotFound, result.FirstError.Code);
    }

    [Fact]
    public async Task Unknown_company_id_returns_not_found()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(Guid.NewGuid(), seed.KeyA), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(GetPublicProductsQueryHandler.ErrorCodes.NotFound, result.FirstError.Code);
    }

    [Fact]
    public async Task Category_filter_narrows_to_one_result()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(seed.CompanyA, seed.KeyA, category: "Ammo"), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Single(result.Value.Items);
        Assert.Equal("Ammo Box", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Search_filter_narrows_to_one_result()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(seed.CompanyA, seed.KeyA, search: "zip"), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Single(result.Value.Items);
        Assert.Equal("Zip Case", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Page_size_one_yields_total_count_two_and_one_item()
    {
        var seed = await SeedAsync(fixture);
        var tenant = CreateTenantContext();
        var handler = CreateHandler(tenant);

        var result = await handler.Handle(Query(seed.CompanyA, seed.KeyA, pageSize: 1), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Single(result.Value.Items);
    }

    private sealed class AnonymousCurrentUserService : ICurrentUserService
    {
        public CurrentUser User => CurrentUser.Anonymous;
    }
}
