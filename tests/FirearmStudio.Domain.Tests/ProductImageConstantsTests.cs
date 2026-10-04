using FirearmStudio.Domain.Common;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class ProductImageConstantsTests
{
    [Fact]
    public void Max_image_bytes_is_five_megabytes()
    {
        Assert.Equal(5 * 1024 * 1024, ProductImageConstants.MaxImageBytes);
    }

    [Fact]
    public void Presign_lifetime_is_sixty_minutes()
    {
        Assert.Equal(60, ProductImageConstants.PresignLifetimeMinutes);
        Assert.Equal(TimeSpan.FromMinutes(60), ProductImageConstants.PresignLifetime);
    }

    [Fact]
    public void Allowed_content_types_are_jpeg_png_and_webp()
    {
        Assert.Equal(
            ["image/jpeg", "image/png", "image/webp"],
            ProductImageConstants.AllowedContentTypes);
    }
}
