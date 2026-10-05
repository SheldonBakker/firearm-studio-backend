using FirearmStudio.Application.Products;
using FirearmStudio.Domain.Entities;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class PublicProductSortTests
{
    private static readonly Product[] Products =
    [
        new() { Id = Guid.NewGuid(), Name = "Bravo", Price = 30m, StockQuantity = 1, Category = "B", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        new() { Id = Guid.NewGuid(), Name = "Alpha", Price = 10m, StockQuantity = 50, Category = "A", CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
        new() { Id = Guid.NewGuid(), Name = "Charlie", Price = 20m, StockQuantity = 9, Category = "C", CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc) },
    ];

    [Theory]
    [InlineData("stockquantity")]
    [InlineData("STOCKQUANTITY")]
    [InlineData("unknown")]
    public void Disallowed_sort_falls_back_to_name(string sortBy)
    {
        var names = ProductSort.ApplyPublic(Products.AsQueryable(), sortBy, "desc")
            .Select(p => p.Name)
            .ToList();

        Assert.Equal(["Charlie", "Bravo", "Alpha"], names);
    }

    [Theory]
    [InlineData("name", "Alpha")]
    [InlineData("price", "Alpha")]
    [InlineData("category", "Alpha")]
    [InlineData("createdat", "Bravo")]
    public void Allowed_sorts_pass_through(string sortBy, string expectedFirst)
    {
        var first = ProductSort.ApplyPublic(Products.AsQueryable(), sortBy, "asc").First();

        Assert.Equal(expectedFirst, first.Name);
    }

    [Fact]
    public void Public_sort_never_orders_by_stock_quantity()
    {
        var byStock = ProductSort.ApplyPublic(Products.AsQueryable(), "stockquantity", "asc")
            .Select(p => p.StockQuantity)
            .ToList();

        Assert.NotEqual([1, 9, 50], byStock);
    }
}
