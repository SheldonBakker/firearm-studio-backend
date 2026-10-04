using FirearmStudio.Application.Products;
using FirearmStudio.Domain.Entities;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class ProductsSortOrderTests
{
    private static Product P(string name, decimal price, string? category, int stock, int createdDaysAgo) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Price = price,
        Category = category,
        StockQuantity = stock,
        CreatedAt = DateTime.UtcNow.AddDays(-createdDaysAgo),
    };

    private static readonly Product[] Sample =
    [
        P("Bravo", 30m, "optics", 5, 1),
        P("Alpha", 10m, "cases", 50, 3),
        P("Charlie", 20m, "ammo", 1, 2),
    ];

    [Fact]
    public void Default_and_unknown_sort_by_fall_back_to_name_ascending()
    {
        var byDefault = ProductSort.Apply(Sample.AsQueryable(), "name", "asc").Select(p => p.Name).ToList();
        var byUnknown = ProductSort.Apply(Sample.AsQueryable(), "banana", "asc").Select(p => p.Name).ToList();

        Assert.Equal(["Alpha", "Bravo", "Charlie"], byDefault);
        Assert.Equal(["Alpha", "Bravo", "Charlie"], byUnknown);
    }

    [Fact]
    public void Unknown_sort_dir_falls_back_to_ascending()
    {
        var names = ProductSort.Apply(Sample.AsQueryable(), "name", "sideways").Select(p => p.Name).ToList();

        Assert.Equal(["Alpha", "Bravo", "Charlie"], names);
    }

    [Fact]
    public void Sort_by_price_both_directions()
    {
        var asc = ProductSort.Apply(Sample.AsQueryable(), "price", "asc").Select(p => p.Price).ToList();
        var desc = ProductSort.Apply(Sample.AsQueryable(), "price", "desc").Select(p => p.Price).ToList();

        Assert.Equal([10m, 20m, 30m], asc);
        Assert.Equal([30m, 20m, 10m], desc);
    }

    [Fact]
    public void Sort_by_category_ascending()
    {
        var categories = ProductSort.Apply(Sample.AsQueryable(), "category", "asc").Select(p => p.Category).ToList();

        Assert.Equal(["ammo", "cases", "optics"], categories);
    }

    [Fact]
    public void Sort_by_stock_quantity_descending()
    {
        var stock = ProductSort.Apply(Sample.AsQueryable(), "stockQuantity", "desc").Select(p => p.StockQuantity).ToList();

        Assert.Equal([50, 5, 1], stock);
    }

    [Fact]
    public void Sort_by_created_at_ascending_orders_oldest_first()
    {
        var names = ProductSort.Apply(Sample.AsQueryable(), "createdAt", "asc").Select(p => p.Name).ToList();

        Assert.Equal(["Alpha", "Charlie", "Bravo"], names);
    }

    [Fact]
    public void Id_is_the_tiebreaker_for_equal_sort_keys()
    {
        var a = P("Same", 10m, null, 0, 0);
        var b = P("Same", 10m, null, 0, 0);
        var items = new[] { a, b };

        var ordered = ProductSort.Apply(items.AsQueryable(), "name", "asc").Select(p => p.Id).ToList();
        var expected = items.OrderBy(p => p.Id).Select(p => p.Id).ToList();

        Assert.Equal(expected, ordered);
    }
}
