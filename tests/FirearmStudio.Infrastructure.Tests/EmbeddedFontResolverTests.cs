using FirearmStudio.Infrastructure.Services.Fonts;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public class EmbeddedFontResolverTests
{
    private readonly EmbeddedFontResolver _resolver = new();

    [Fact]
    public void ResolveTypeface_regular_maps_to_the_regular_face()
    {
        var info = _resolver.ResolveTypeface(EmbeddedFontResolver.FamilyName, bold: false, italic: false);

        Assert.NotNull(info);
        Assert.Equal("Roboto#regular", info!.FaceName);
        Assert.False(info.MustSimulateBold);
        Assert.False(info.MustSimulateItalic);
    }

    [Fact]
    public void ResolveTypeface_bold_maps_to_the_bold_face_without_simulation()
    {
        var info = _resolver.ResolveTypeface(EmbeddedFontResolver.FamilyName, bold: true, italic: false);

        Assert.NotNull(info);
        Assert.Equal("Roboto#bold", info!.FaceName);
        Assert.False(info.MustSimulateBold);
    }

    [Fact]
    public void ResolveTypeface_italic_reuses_the_regular_face_and_asks_for_simulation()
    {
        var info = _resolver.ResolveTypeface(EmbeddedFontResolver.FamilyName, bold: false, italic: true);

        Assert.NotNull(info);
        Assert.Equal("Roboto#regular", info!.FaceName);
        Assert.True(info.MustSimulateItalic);
    }

    [Theory]
    [InlineData("Arial")]
    [InlineData("Verdana")]
    [InlineData("")]
    public void ResolveTypeface_falls_back_to_the_embedded_family_for_any_other_request(string family)
    {
        var info = _resolver.ResolveTypeface(family, bold: false, italic: false);

        Assert.NotNull(info);
        Assert.Equal("Roboto#regular", info!.FaceName);
    }

    [Theory]
    [InlineData("Roboto#regular")]
    [InlineData("Roboto#bold")]
    public void GetFont_returns_a_truetype_payload_for_each_face(string faceName)
    {
        var bytes = _resolver.GetFont(faceName);

        Assert.NotNull(bytes);
        Assert.True(bytes!.Length > 100_000, $"Expected a real TTF, got {bytes.Length} bytes.");
        Assert.Equal(new byte[] { 0x00, 0x01, 0x00, 0x00 }, bytes.Take(4).ToArray());
    }

    [Fact]
    public void GetFont_returns_distinct_payloads_for_regular_and_bold()
    {
        var regular = _resolver.GetFont("Roboto#regular");
        var bold = _resolver.GetFont("Roboto#bold");

        Assert.NotNull(regular);
        Assert.NotNull(bold);
        Assert.NotEqual(regular!.Length, bold!.Length);
    }

    [Fact]
    public void GetFont_returns_the_regular_payload_for_an_unknown_face()
    {
        var bytes = _resolver.GetFont("something-else");

        Assert.NotNull(bytes);
        Assert.True(bytes!.Length > 100_000);
    }
}
