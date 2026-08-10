using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Common;
using FirearmStudio.Application.Registers;
using FirearmStudio.Infrastructure.Services.Fonts;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace FirearmStudio.Infrastructure.Services;

public sealed class PdfSharpRegisterRenderer : IRegisterPdfRenderer
{
    private const string FontFamily = EmbeddedFontResolver.FamilyName;
    private const double BodyFontSize = 8;
    private const double TitleFontSize = 12;
    private const double CompanyFontSize = 14;
    private const double MarginPoints = 24;
    private const double PageWidthMm = 297;
    private const double PageHeightMm = 210;
    private const double BorderWidth = 0.5;
    private const double CellPadding = 3;
    private const double MigraDocCellPaddingPoints = 3.4016;
    private const double CellChromePoints = BorderWidth + CellPadding + MigraDocCellPaddingPoints;

    private const double HeaderDistancePoints = MarginPoints;
    private const double FooterDistancePoints = MarginPoints;
    private const double HeaderMinimumReservedHeightPoints = 91;
    private const double HeaderSafetyPadPoints = 1;
    private const double TitleSpaceBeforePoints = 6;
    private const double HeaderSpaceAfterPoints = 8;
    private const double FooterReservedHeightPoints = 19;
    private const double BottomMarginPoints = FooterDistancePoints + FooterReservedHeightPoints;

    private static readonly Color HeaderFill = new(238, 238, 238);

    private static readonly Lock RenderGate = new();

    private static RegisterTextMeasurer? Measurer;

    static PdfSharpRegisterRenderer()
    {
        GlobalFontSettings.FontResolver = new EmbeddedFontResolver();
    }

    public byte[] Render(RegisterDocument document)
    {
        lock (RenderGate)
        {
            Measurer ??= new RegisterTextMeasurer();
            Measurer.ResetMeasurementCache();

            var pdf = new PdfDocumentRenderer { Document = Compose(document, Measurer) };
            pdf.RenderDocument();

            var generatedAtUtc = TimeZoneInfo.ConvertTimeToUtc(document.GeneratedAt, SouthAfricaTimeZone.Instance);
            pdf.PdfDocument.Info.CreationDate = generatedAtUtc;
            pdf.PdfDocument.Info.ModificationDate = generatedAtUtc;

            using var stream = new MemoryStream();
            pdf.PdfDocument.Save(stream, false);
            return stream.ToArray();
        }
    }

    private static Document Compose(RegisterDocument source, RegisterTextMeasurer measurer)
    {
        var document = new Document();
        document.Info.Title = source.Title;
        document.Info.Author = source.CompanyName;

        var normal = document.Styles[StyleNames.Normal]
            ?? throw new InvalidOperationException("MigraDoc is missing its Normal style.");
        normal.Font.Name = FontFamily;
        normal.Font.Size = BodyFontSize;

        var section = document.AddSection();

        section.PageSetup.PageWidth = Unit.FromMillimeter(PageWidthMm);
        section.PageSetup.PageHeight = Unit.FromMillimeter(PageHeightMm);
        section.PageSetup.LeftMargin = Unit.FromPoint(MarginPoints);
        section.PageSetup.RightMargin = Unit.FromPoint(MarginPoints);

        section.PageSetup.HeaderDistance = Unit.FromPoint(HeaderDistancePoints);
        section.PageSetup.FooterDistance = Unit.FromPoint(FooterDistancePoints);
        section.PageSetup.BottomMargin = Unit.FromPoint(BottomMarginPoints);

        var headerParagraphs = HeaderParagraphs(source);

        section.PageSetup.TopMargin = Unit.FromPoint(
            HeaderDistancePoints
            + HeaderReservedHeight(headerParagraphs, measurer, ContentWidth(section).Point));

        ComposeHeader(section, headerParagraphs);
        ComposeFooter(section, source);
        ComposeContent(section, source, measurer);

        return document;
    }

    private static List<HeaderParagraph> HeaderParagraphs(RegisterDocument source)
    {
        var paragraphs = new List<HeaderParagraph>(6)
        {
            new(RegisterCellText.Sanitise(source.CompanyName), CompanyFontSize, true, 0, 0),
        };

        if (!string.IsNullOrWhiteSpace(source.CompanyRegistrationNumber))
        {
            paragraphs.Add(new(
                RegisterCellText.Sanitise($"Registration No: {source.CompanyRegistrationNumber}"),
                BodyFontSize, false, 0, 0));
        }

        if (!string.IsNullOrWhiteSpace(source.CompanyAddress))
        {
            paragraphs.Add(new(RegisterCellText.Sanitise(source.CompanyAddress), BodyFontSize, false, 0, 0));
        }

        paragraphs.Add(new(
            RegisterCellText.Sanitise(source.Title), TitleFontSize, true, TitleSpaceBeforePoints, 0));

        paragraphs.Add(new(
            RegisterCellText.Sanitise($"Period: {source.PeriodFrom:yyyy-MM-dd} to {source.PeriodTo:yyyy-MM-dd}"),
            BodyFontSize, false, 0, 0));

        paragraphs.Add(new(
            RegisterCellText.Sanitise(
                $"Generated {source.GeneratedAt:yyyy-MM-dd HH:mm} (SAST) by {source.GeneratedBy}"),
            BodyFontSize, false, 0, HeaderSpaceAfterPoints));

        return paragraphs;
    }

    private static double HeaderReservedHeight(
        IReadOnlyList<HeaderParagraph> paragraphs, RegisterTextMeasurer measurer, double contentWidth)
    {
        var height = 0d;

        foreach (var paragraph in paragraphs)
        {
            height += paragraph.SpaceBefore + paragraph.SpaceAfter;
            height += measurer.LineCount(paragraph.Text, paragraph.FontSize, paragraph.Bold, contentWidth)
                * measurer.LineHeight(paragraph.FontSize, paragraph.Bold);
        }

        return Math.Max(HeaderMinimumReservedHeightPoints, height + HeaderSafetyPadPoints);
    }

    private static void ComposeHeader(Section section, IReadOnlyList<HeaderParagraph> paragraphs)
    {
        var header = section.Headers.Primary;

        foreach (var item in paragraphs)
        {
            var paragraph = header.AddParagraph(item.Text);
            paragraph.Format.Font.Size = item.FontSize;
            paragraph.Format.Font.Bold = item.Bold;

            if (item.SpaceBefore > 0)
            {
                paragraph.Format.SpaceBefore = Unit.FromPoint(item.SpaceBefore);
            }

            if (item.SpaceAfter > 0)
            {
                paragraph.Format.SpaceAfter = Unit.FromPoint(item.SpaceAfter);
            }
        }
    }

    private static void ComposeFooter(Section section, RegisterDocument source)
    {
        var footer = section.Footers.Primary;

        var paragraph = footer.AddParagraph(RegisterCellText.Sanitise($"Total rows: {source.Rows.Count}"));
        paragraph.Format.TabStops.ClearAll();
        paragraph.Format.TabStops.AddTabStop(ContentWidth(section), TabAlignment.Right);
        paragraph.AddTab();
        paragraph.AddText("Page ");
        paragraph.AddPageField();
        paragraph.AddText(" of ");
        paragraph.AddNumPagesField();
    }

    private static void ComposeContent(Section section, RegisterDocument source, RegisterTextMeasurer measurer)
    {
        if (source.Rows.Count == 0 || source.Columns.Count == 0)
        {
            var empty = section.AddParagraph(RegisterCellText.Sanitise(source.EmptyStateText));
            empty.Format.Font.Italic = true;
            empty.Format.SpaceBefore = Unit.FromPoint(12);
            return;
        }

        var table = section.AddTable();
        table.Borders.Width = BorderWidth;
        table.Borders.Color = Colors.Black;

        table.Rows.LeftIndent = Unit.Zero;

        var widths = RegisterTableLayout.ColumnWidths(
            source.Columns.Count, source.ColumnWeights, ContentWidth(section).Point);

        var breakWidths = new double[widths.Length];

        for (var i = 0; i < widths.Length; i++)
        {
            breakWidths[i] = widths[i] - CellChromePoints;

            var column = table.AddColumn(Unit.FromPoint(widths[i]));
            column.Format.LeftIndent = Unit.FromPoint(CellPadding);
            column.Format.RightIndent = Unit.FromPoint(CellPadding);
        }

        var measureHeading = (string segment) => measurer.Width(segment, BodyFontSize, true);
        var measureBody = (string segment) => measurer.Width(segment, BodyFontSize, false);

        var headerRow = table.AddRow();
        headerRow.HeadingFormat = true;
        headerRow.Format.Font.Bold = true;
        headerRow.Shading.Color = HeaderFill;
        headerRow.VerticalAlignment = VerticalAlignment.Center;

        for (var i = 0; i < source.Columns.Count; i++)
        {
            AddCellText(headerRow.Cells[i], source.Columns[i], breakWidths[i], measureHeading);
        }

        foreach (var row in source.Rows)
        {
            var tableRow = table.AddRow();
            tableRow.VerticalAlignment = VerticalAlignment.Center;

            for (var i = 0; i < source.Columns.Count; i++)
            {
                AddCellText(
                    tableRow.Cells[i], i < row.Length ? row[i] : string.Empty, breakWidths[i], measureBody);
            }
        }
    }

    private static void AddCellText(Cell cell, string? value, double breakWidth, Func<string, double> measure)
    {
        var text = RegisterCellText.InsertBreakOpportunities(
            RegisterCellText.Sanitise(value), breakWidth, measure);

        var paragraph = cell.AddParagraph(text);
        paragraph.Format.SpaceBefore = Unit.FromPoint(CellPadding);
        paragraph.Format.SpaceAfter = Unit.FromPoint(CellPadding);
    }

    private static Unit ContentWidth(Section section) =>
        section.PageSetup.PageWidth
        - section.PageSetup.LeftMargin
        - section.PageSetup.RightMargin;

    private readonly record struct HeaderParagraph(
        string Text, double FontSize, bool Bold, double SpaceBefore, double SpaceAfter);
}
