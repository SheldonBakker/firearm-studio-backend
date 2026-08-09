using System.Reflection;
using PdfSharp.Fonts;

namespace FirearmStudio.Infrastructure.Services.Fonts;

/// <summary>
/// Serves the assembly's embedded Roboto faces for every font request. The production image is
/// a chiseled container with no system fonts, so returning null for an unrecognised family would
/// surface as a runtime failure halfway through a register export; this resolver never does.
/// </summary>
public sealed class EmbeddedFontResolver : IFontResolver
{
    public const string FamilyName = "Roboto";

    private const string RegularFace = "Roboto#regular";
    private const string BoldFace = "Roboto#bold";

    private const string RegularResource = "FirearmStudio.Infrastructure.Fonts.Roboto-Regular.ttf";
    private const string BoldResource = "FirearmStudio.Infrastructure.Fonts.Roboto-Bold.ttf";

    // Read once and shared: PDFsharp caches resolved faces, but GetFont can still be called
    // concurrently on first use from parallel export requests.
    private static readonly byte[] Regular = LoadResource(RegularResource);
    private static readonly byte[] Bold = LoadResource(BoldResource);

    // PDFsharp implements italic simulation but NOT bold simulation, so bold must be a real face.
    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
        bold
            ? new FontResolverInfo(BoldFace, false, italic)
            : new FontResolverInfo(RegularFace, false, italic);

    public byte[]? GetFont(string faceName) =>
        faceName == BoldFace ? Bold : Regular;

    private static byte[] LoadResource(string resourceName)
    {
        var assembly = typeof(EmbeddedFontResolver).Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded font '{resourceName}' is missing. Available resources: " +
                string.Join(", ", assembly.GetManifestResourceNames()));

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
