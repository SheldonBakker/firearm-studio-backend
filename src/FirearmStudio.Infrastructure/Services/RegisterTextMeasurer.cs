using FirearmStudio.Infrastructure.Services.Fonts;
using PdfSharp.Drawing;

namespace FirearmStudio.Infrastructure.Services;

internal sealed class RegisterTextMeasurer
{
    private static readonly XSize MeasureSurface = new(2000, 2000);

    private readonly XGraphics _graphics = XGraphics.CreateMeasureContext(
        MeasureSurface, XGraphicsUnit.Point, XPageDirection.Downwards);

    private readonly Dictionary<(double Size, bool Bold), XFont> _fonts = [];

    private readonly Dictionary<(string Text, double Size, bool Bold), double> _widths = [];

    public void ResetMeasurementCache() => _widths.Clear();

    public double Width(string text, double fontSize, bool bold)
    {
        var key = (text, fontSize, bold);

        if (_widths.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var width = _graphics.MeasureString(text, Font(fontSize, bold)).Width;
        _widths[key] = width;
        return width;
    }

    public double LineHeight(double fontSize, bool bold) => Font(fontSize, bold).GetHeight();

    public int LineCount(string text, double fontSize, bool bold, double availableWidth)
    {
        if (string.IsNullOrEmpty(text) || availableWidth <= 0)
        {
            return 1;
        }

        var lines = 1;
        var current = string.Empty;

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.Length == 0)
            {
                current = word;
                continue;
            }

            var candidate = $"{current} {word}";

            if (Width(candidate, fontSize, bold) <= availableWidth)
            {
                current = candidate;
                continue;
            }

            lines++;
            current = word;
        }

        return lines;
    }

    private XFont Font(double fontSize, bool bold)
    {
        var key = (fontSize, bold);

        if (_fonts.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var font = new XFont(
            EmbeddedFontResolver.FamilyName,
            fontSize,
            bold ? XFontStyleEx.Bold : XFontStyleEx.Regular);

        _fonts[key] = font;
        return font;
    }
}
