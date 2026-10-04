using FirearmStudio.Application.Products;
using FirearmStudio.Application.Products.CreateProduct;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class CreateProductRequestValidatorTests
{
    private static readonly CreateProductRequestValidator Validator = new();

    private static CreateProductRequest Valid() =>
        new("Rifle Case", null, null, 100m, null, null, 0, true);

    [Fact]
    public void Valid_request_passes()
    {
        Assert.True(Validator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Name_is_required()
    {
        Assert.False(Validator.Validate(Valid() with { Name = "" }).IsValid);
    }

    [Fact]
    public void Name_over_two_hundred_is_rejected()
    {
        Assert.False(Validator.Validate(Valid() with { Name = new string('x', 201) }).IsValid);
    }

    [Fact]
    public void Whitespace_only_sku_is_rejected()
    {
        Assert.False(Validator.Validate(Valid() with { Sku = "   " }).IsValid);
    }

    [Fact]
    public void Negative_price_cost_and_stock_are_rejected()
    {
        Assert.False(Validator.Validate(Valid() with { Price = -1m }).IsValid);
        Assert.False(Validator.Validate(Valid() with { CostPrice = -1m }).IsValid);
        Assert.False(Validator.Validate(Valid() with { StockQuantity = -1 }).IsValid);
    }

    [Fact]
    public void Price_and_cost_must_fit_numeric_12_2()
    {
        Assert.False(Validator.Validate(Valid() with { Price = 10_000_000_000m }).IsValid);
        Assert.False(Validator.Validate(Valid() with { Price = 1.999m }).IsValid);
        Assert.True(Validator.Validate(Valid() with { Price = 9_999_999_999.99m }).IsValid);
        Assert.False(Validator.Validate(Valid() with { CostPrice = 10_000_000_000m }).IsValid);
        Assert.False(Validator.Validate(Valid() with { CostPrice = 1.999m }).IsValid);
        Assert.True(Validator.Validate(Valid() with { CostPrice = 9_999_999_999.99m }).IsValid);
    }
}
