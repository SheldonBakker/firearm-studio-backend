using System.Reflection;
using PdfSharp.Fonts;

namespace FirearmStudio.Infrastructure.Services.Fonts;

public sealed class EmbeddedFontResolver : IFontResolver
{
    public const string FamilyName = "Roboto";

    private const string RegularFace = "Roboto#regular";
    private const string BoldFace = "Roboto#bold";

    private const string RegularResource = "FirearmStudio.Infrastructure.Fonts.Roboto-Regular.ttf";
    private const string BoldResource = "FirearmStudio.Infrastructure.Fonts.Roboto-Bold.ttf";

    private static readonly byte[] Regular = LoadResource(RegularResource);
    private static readonly byte[] Bold = LoadResource(BoldResource);

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
