using FirearmStudio.Application.Common;
using FirearmStudio.Application.Products.UploadProductImage;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class ProductImageUploadTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02, 0x03, 0x04];

    private static async Task<(Guid company, Guid productId)> SeedProductAsync(TestDatabaseFixture fixture)
    {
        var company = Guid.NewGuid();
        await using var seed = fixture.CreateDbContext();
        await seed.Database.MigrateAsync();
        seed.Companies.Add(new Company { Id = company, Name = "C" });
        var product = new Product { Name = "P", Price = 10m };
        product.CompanyId = company;
        seed.Products.Add(product);
        await seed.SaveChangesAsync();
        return (company, product.Id);
    }

    [Fact]
    public async Task Length_zero_or_over_the_cap_returns_too_large_before_touching_storage()
    {
        var (company, productId) = await SeedProductAsync(fixture);
        var storage = new FakeFileStorage();

        await using var db = fixture.CreateDbContext(company);
        var handler = new UploadProductImageCommandHandler(db, storage, NullLogger<UploadProductImageCommandHandler>.Instance);

        var zero = await handler.Handle(
            new UploadProductImageCommand(productId, new MemoryStream(), "image/jpeg", 0), CancellationToken.None);
        var over = await handler.Handle(
            new UploadProductImageCommand(productId, new MemoryStream(JpegBytes), "image/jpeg", ProductImageConstants.MaxImageBytes + 1),
            CancellationToken.None);

        Assert.Equal(UploadProductImageCommandHandler.ErrorCodes.TooLarge, zero.FirstError.Code);
        Assert.Equal(UploadProductImageCommandHandler.ErrorCodes.TooLarge, over.FirstError.Code);
        Assert.Empty(storage.Uploaded);
    }

    [Fact]
    public async Task Jpg_alias_with_jpeg_bytes_is_accepted()
    {
        var (company, productId) = await SeedProductAsync(fixture);
        var storage = new FakeFileStorage();

        await using var db = fixture.CreateDbContext(company);
        var handler = new UploadProductImageCommandHandler(db, storage, NullLogger<UploadProductImageCommandHandler>.Instance);

        var result = await handler.Handle(
            new UploadProductImageCommand(productId, new MemoryStream(JpegBytes), "image/jpg", JpegBytes.Length),
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Single(storage.Uploaded);
    }

    [Fact]
    public async Task Declared_png_with_jpeg_bytes_is_rejected_as_invalid()
    {
        var (company, productId) = await SeedProductAsync(fixture);
        var storage = new FakeFileStorage();

        await using var db = fixture.CreateDbContext(company);
        var handler = new UploadProductImageCommandHandler(db, storage, NullLogger<UploadProductImageCommandHandler>.Instance);

        var result = await handler.Handle(
            new UploadProductImageCommand(productId, new MemoryStream(JpegBytes), "image/png", JpegBytes.Length),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(UploadProductImageCommandHandler.ErrorCodes.InvalidImage, result.FirstError.Code);
        Assert.Empty(storage.Uploaded);
    }

    [Fact]
    public async Task Second_upload_best_effort_deletes_the_superseded_object()
    {
        var (company, productId) = await SeedProductAsync(fixture);
        var storage = new FakeFileStorage();

        await using (var db = fixture.CreateDbContext(company))
        {
            var handler = new UploadProductImageCommandHandler(db, storage, NullLogger<UploadProductImageCommandHandler>.Instance);
            await handler.Handle(
                new UploadProductImageCommand(productId, new MemoryStream(JpegBytes), "image/jpeg", JpegBytes.Length),
                CancellationToken.None);
        }

        string firstKey = storage.Uploaded[0];

        await using (var db = fixture.CreateDbContext(company))
        {
            var handler = new UploadProductImageCommandHandler(db, storage, NullLogger<UploadProductImageCommandHandler>.Instance);
            await handler.Handle(
                new UploadProductImageCommand(productId, new MemoryStream(JpegBytes), "image/jpeg", JpegBytes.Length),
                CancellationToken.None);
        }

        Assert.Equal(2, storage.Uploaded.Count);
        Assert.Contains(firstKey, storage.Deleted);
    }

    [Fact]
    public async Task Storage_failure_on_upload_maps_to_the_upstream_failure_type()
    {
        var (company, productId) = await SeedProductAsync(fixture);
        var storage = new FakeFileStorage { FailUploads = true };

        await using var db = fixture.CreateDbContext(company);
        var handler = new UploadProductImageCommandHandler(db, storage, NullLogger<UploadProductImageCommandHandler>.Instance);

        var result = await handler.Handle(
            new UploadProductImageCommand(productId, new MemoryStream(JpegBytes), "image/jpeg", JpegBytes.Length),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(UpstreamErrorTypes.UpstreamFailure, (int)result.FirstError.Type);
        Assert.Equal(UploadProductImageCommandHandler.ErrorCodes.StorageUnavailable, result.FirstError.Code);
    }

    [Fact]
    public async Task Body_larger_than_declared_length_and_over_the_cap_returns_too_large()
    {
        var (company, productId) = await SeedProductAsync(fixture);
        var storage = new FakeFileStorage();

        await using var db = fixture.CreateDbContext(company);
        var handler = new UploadProductImageCommandHandler(db, storage, NullLogger<UploadProductImageCommandHandler>.Instance);

        var body = new byte[ProductImageConstants.MaxImageBytes + 1];
        JpegBytes.CopyTo(body, 0);

        var result = await handler.Handle(
            new UploadProductImageCommand(productId, new MemoryStream(body), "image/jpeg", 10),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(UploadProductImageCommandHandler.ErrorCodes.TooLarge, result.FirstError.Code);
        Assert.Empty(storage.Uploaded);
    }

    [Fact]
    public async Task Body_exactly_at_the_cap_is_accepted()
    {
        var (company, productId) = await SeedProductAsync(fixture);
        var storage = new FakeFileStorage();

        await using var db = fixture.CreateDbContext(company);
        var handler = new UploadProductImageCommandHandler(db, storage, NullLogger<UploadProductImageCommandHandler>.Instance);

        var body = new byte[ProductImageConstants.MaxImageBytes];
        JpegBytes.CopyTo(body, 0);

        var result = await handler.Handle(
            new UploadProductImageCommand(productId, new MemoryStream(body), "image/jpeg", ProductImageConstants.MaxImageBytes),
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Single(storage.Uploaded);
    }
}
