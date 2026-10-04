using FirearmStudio.Application.Products;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class ProductImageKeyTests
{
    [Fact]
    public void Build_uses_the_company_and_product_prefix_with_a_guid_segment()
    {
        var company = Guid.NewGuid();
        var product = Guid.NewGuid();

        var key = ProductImageKey.Build(company, product, "image/jpeg");

        var prefix = $"companies/{company}/products/{product}/";
        Assert.StartsWith(prefix, key);

        var lastSegment = key[prefix.Length..];
        var dot = lastSegment.LastIndexOf('.');
        Assert.True(dot > 0);
        Assert.True(Guid.TryParse(lastSegment[..dot], out _));
        Assert.Equal("jpg", lastSegment[(dot + 1)..]);
    }

    [Theory]
    [InlineData("image/jpeg", "jpg")]
    [InlineData("image/png", "png")]
    [InlineData("image/webp", "webp")]
    public void Extension_maps_each_allowed_content_type(string contentType, string expected)
    {
        Assert.Equal(expected, ProductImageKey.Extension(contentType));
    }

    [Fact]
    public void Extension_throws_for_an_unknown_content_type()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProductImageKey.Extension("image/gif"));
    }
}
