using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Products;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class ProductResponseMapperTests
{
    private sealed class StubFileStorage : IFileStorage
    {
        public Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct) => Task.CompletedTask;

        public Task DeleteAsync(string key, CancellationToken ct) => Task.CompletedTask;

        public string GetPresignedReadUrl(string key, TimeSpan lifetime) =>
            $"https://files.test/{key}?ttl={lifetime.TotalMinutes}";
    }

    private static ProductRow Row(string? imageKey) => new(
        Guid.NewGuid(), "Rifle Case", null, null, 100m, null, null, 0, imageKey, true, DateTime.UtcNow, null);

    [Fact]
    public void Map_sets_image_url_using_the_presign_lifetime_when_image_key_is_present()
    {
        var response = ProductResponseMapper.Map(Row("companies/a/products/b/x.jpg"), new StubFileStorage());

        Assert.Equal("https://files.test/companies/a/products/b/x.jpg?ttl=60", response.ImageUrl);
    }

    [Fact]
    public void Map_leaves_image_url_null_when_image_key_is_absent()
    {
        var response = ProductResponseMapper.Map(Row(null), new StubFileStorage());

        Assert.Null(response.ImageUrl);
    }

    [Fact]
    public void ProductResponse_does_not_expose_the_image_key()
    {
        Assert.Null(typeof(ProductResponse).GetProperty("ImageKey"));
    }
}
