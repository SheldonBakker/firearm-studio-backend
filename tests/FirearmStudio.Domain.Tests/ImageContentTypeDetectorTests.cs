using FirearmStudio.Application.Products;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class ImageContentTypeDetectorTests
{
    [Fact]
    public void Detects_jpeg_from_magic_bytes()
    {
        byte[] head = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        Assert.Equal("image/jpeg", ImageContentTypeDetector.Detect(head));
    }

    [Fact]
    public void Detects_png_from_magic_bytes()
    {
        byte[] head = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
        Assert.Equal("image/png", ImageContentTypeDetector.Detect(head));
    }

    [Fact]
    public void Detects_webp_from_riff_and_webp_markers()
    {
        byte[] head = [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50];
        Assert.Equal("image/webp", ImageContentTypeDetector.Detect(head));
    }

    [Fact]
    public void Returns_null_for_a_text_buffer()
    {
        byte[] head = [0x68, 0x65, 0x6C, 0x6C, 0x6F];
        Assert.Null(ImageContentTypeDetector.Detect(head));
    }

    [Fact]
    public void Returns_null_for_a_too_short_buffer()
    {
        byte[] head = [0xFF, 0xD8];
        Assert.Null(ImageContentTypeDetector.Detect(head));
    }

    [Fact]
    public void Returns_null_for_a_riff_container_that_is_not_webp()
    {
        byte[] head = [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x41, 0x56, 0x49, 0x20];
        Assert.Null(ImageContentTypeDetector.Detect(head));
    }
}
