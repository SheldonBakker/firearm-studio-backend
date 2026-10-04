using FirearmStudio.Application.Model;
using FirearmStudio.Application.Products;
using FirearmStudio.Application.Products.UpdateProduct;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class UpdateProductRequestValidatorTests
{
    private static readonly UpdateProductRequestValidator Validator = new();

    private static UpdateProductRequest Empty() =>
        new(default, default, default, default, default, default, default, default);

    [Fact]
    public void Empty_request_is_rejected()
    {
        Assert.False(Validator.Validate(Empty()).IsValid);
    }

    [Fact]
    public void Set_name_empty_is_rejected()
    {
        var request = Empty() with { Name = new Optional<string>("") };
        Assert.False(Validator.Validate(request).IsValid);
    }

    [Fact]
    public void Set_price_negative_is_rejected()
    {
        var request = Empty() with { Price = new Optional<decimal>(-1m) };
        Assert.False(Validator.Validate(request).IsValid);
    }

    [Fact]
    public void Set_whitespace_sku_is_rejected()
    {
        var request = Empty() with { Sku = new Optional<string?>("   ") };
        Assert.False(Validator.Validate(request).IsValid);
    }

    [Fact]
    public void A_single_valid_field_passes_and_unset_bounds_are_not_checked()
    {
        var request = Empty() with { Name = new Optional<string>("Updated") };
        Assert.True(Validator.Validate(request).IsValid);
    }

    [Fact]
    public void Set_price_and_cost_must_fit_numeric_12_2()
    {
        Assert.False(Validator.Validate(Empty() with { Price = new Optional<decimal>(10_000_000_000m) }).IsValid);
        Assert.False(Validator.Validate(Empty() with { Price = new Optional<decimal>(1.999m) }).IsValid);
        Assert.True(Validator.Validate(Empty() with { Price = new Optional<decimal>(9_999_999_999.99m) }).IsValid);
        Assert.False(Validator.Validate(Empty() with { CostPrice = new Optional<decimal?>(10_000_000_000m) }).IsValid);
        Assert.False(Validator.Validate(Empty() with { CostPrice = new Optional<decimal?>(1.999m) }).IsValid);
        Assert.True(Validator.Validate(Empty() with { CostPrice = new Optional<decimal?>(9_999_999_999.99m) }).IsValid);
    }
}
