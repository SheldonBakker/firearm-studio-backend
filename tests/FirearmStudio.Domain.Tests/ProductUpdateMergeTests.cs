using FirearmStudio.Application.Model;
using FirearmStudio.Application.Products;
using FirearmStudio.Domain.Entities;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class ProductUpdateMergeTests
{
    private static Product Existing() => new()
    {
        Name = "Original",
        Category = "optics",
        Sku = "ABC",
    };

    [Fact]
    public void Set_null_optional_clears_the_column()
    {
        var product = Existing();
        var request = new UpdateProductRequest(
            default, default, default, default, default, new Optional<string?>(null), default, default);

        request.Category.ApplyTo(v => product.Category = string.IsNullOrWhiteSpace(v) ? null : v.Trim());

        Assert.Null(product.Category);
    }

    [Fact]
    public void Unset_optional_leaves_the_value_unchanged()
    {
        var product = Existing();
        var request = new UpdateProductRequest(
            default, default, default, default, default, default, default, default);

        request.Category.ApplyTo(v => product.Category = v);

        Assert.Equal("optics", product.Category);
    }

    [Fact]
    public void Set_value_overwrites_and_is_trimmed()
    {
        var product = Existing();
        var request = new UpdateProductRequest(
            new Optional<string>("  Renamed  "), default, default, default, default, default, default, default);

        request.Name.ApplyTo(v => product.Name = v.Trim());

        Assert.Equal("Renamed", product.Name);
    }
}
