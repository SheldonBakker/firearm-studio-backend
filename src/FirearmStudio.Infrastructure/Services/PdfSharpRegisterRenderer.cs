using FirearmStudio.Application.Abstractions;
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
    private const double MarginPoints = 24;
    private const double PageWidthMm = 297;    // A4 landscape
    private const double PageHeightMm = 210;
    private const double BorderWidth = 0.5;
    private const double CellPadding = 3;

    private static readonly Color HeaderFill = new(238, 238, 238);

    static PdfSharpRegisterRenderer()
    {
        // PDFsharp allows the font resolver to be set exactly once per process, and never after
        // the first XFont exists. A static constructor is the only place that holds for a
        // singleton service resolved from concurrent request threads.
        GlobalFontSettings.FontResolver = new EmbeddedFontResolver();
    }

    public byte[] Render(RegisterDocument document)
    {
        var pdf = new PdfDocumentRenderer { Document = Compose(document) };
        pdf.RenderDocument();

        using var stream = new MemoryStream();
        pdf.PdfDocument.Save(stream, false);
        return stream.ToArray();
    }

    private static Document Compose(RegisterDocument source)
    {
        var document = new Document();
        document.Info.Title = source.Title;
        document.Info.Author = source.CompanyName;

        var normal = document.Styles[StyleNames.Normal]
            ?? throw new InvalidOperationException("MigraDoc is missing its Normal style.");
        normal.Font.Name = FontFamily;
        normal.Font.Size = BodyFontSize;

        var section = document.AddSection();

        // Explicit landscape dimensions, deliberately NOT PageFormat + Orientation. In 6.2.4
        // PageFormat alone leaves PageWidth reading 0, which would feed negative widths to
        // every column, and Orientation is ignored once width and height are set.
        section.PageSetup.PageWidth = Unit.FromMillimeter(PageWidthMm);
        section.PageSetup.PageHeight = Unit.FromMillimeter(PageHeightMm);
        section.PageSetup.LeftMargin = Unit.FromPoint(MarginPoints);
        section.PageSetup.RightMargin = Unit.FromPoint(MarginPoints);
        section.PageSetup.TopMargin = Unit.FromPoint(MarginPoints);
        section.PageSetup.BottomMargin = Unit.FromPoint(MarginPoints);

        ComposeHeader(section, source);
        ComposeFooter(section, source);
        ComposeContent(section, source);

        return document;
    }

    private static void ComposeHeader(Section section, RegisterDocument source)
    {
        var header = section.Headers.Primary;

        var company = header.AddParagraph(RegisterCellText.Sanitise(source.CompanyName));
        company.Format.Font.Size = 14;
        company.Format.Font.Bold = true;

        if (!string.IsNullOrWhiteSpace(source.CompanyRegistrationNumber))
        {
            header.AddParagraph(
                RegisterCellText.Sanitise($"Registration No: {source.CompanyRegistrationNumber}"));
        }

        if (!string.IsNullOrWhiteSpace(source.CompanyAddress))
        {
            header.AddParagraph(RegisterCellText.Sanitise(source.CompanyAddress));
        }

        var title = header.AddParagraph(RegisterCellText.Sanitise(source.Title));
        title.Format.Font.Size = 12;
        title.Format.Font.Bold = true;
        title.Format.SpaceBefore = Unit.FromPoint(6);

        header.AddParagraph($"Period: {source.PeriodFrom:yyyy-MM-dd} to {source.PeriodTo:yyyy-MM-dd}");

        var generated = header.AddParagraph(RegisterCellText.Sanitise(
            $"Generated {source.GeneratedAt:yyyy-MM-dd HH:mm} (SAST) by {source.GeneratedBy}"));
        generated.Format.SpaceAfter = Unit.FromPoint(8);
    }

    private static void ComposeFooter(Section section, RegisterDocument source)
    {
        var footer = section.Footers.Primary;

        var paragraph = footer.AddParagraph($"Total rows: {source.Rows.Count}");
        paragraph.Format.TabStops.ClearAll();
        paragraph.Format.TabStops.AddTabStop(ContentWidth(section), TabAlignment.Right);
        paragraph.AddTab();
        paragraph.AddText("Page ");
        paragraph.AddPageField();
        paragraph.AddText(" of ");
        paragraph.AddNumPagesField();
    }

    private static void ComposeContent(Section section, RegisterDocument source)
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

        var widths = RegisterTableLayout.ColumnWidths(
            source.Columns.Count, source.ColumnWeights, ContentWidth(section).Point);

        foreach (var width in widths)
        {
            var column = table.AddColumn(Unit.FromPoint(width));
            column.Format.LeftIndent = Unit.FromPoint(CellPadding);
            column.Format.RightIndent = Unit.FromPoint(CellPadding);
        }

        var headerRow = table.AddRow();
        headerRow.HeadingFormat = true;   // repeats the header on every page
        headerRow.Format.Font.Bold = true;
        headerRow.Shading.Color = HeaderFill;
        headerRow.VerticalAlignment = VerticalAlignment.Center;

        for (var i = 0; i < source.Columns.Count; i++)
        {
            AddCellText(headerRow.Cells[i], source.Columns[i]);
        }

        foreach (var row in source.Rows)
        {
            var tableRow = table.AddRow();
            tableRow.VerticalAlignment = VerticalAlignment.Center;

            // A malformed row must not abort a compliance export, so extra cells are dropped
            // and missing ones render blank.
            for (var i = 0; i < source.Columns.Count; i++)
            {
                AddCellText(tableRow.Cells[i], i < row.Length ? row[i] : string.Empty);
            }
        }
    }

    private static void AddCellText(Cell cell, string? value)
    {
        var paragraph = cell.AddParagraph(RegisterCellText.Sanitise(value));
        paragraph.Format.SpaceBefore = Unit.FromPoint(CellPadding);
        paragraph.Format.SpaceAfter = Unit.FromPoint(CellPadding);
    }

    // PageWidth is authoritative only because Compose sets it explicitly. EffectivePageWidth
    // is [Obsolete] in 6.2.4 and will not compile under TreatWarningsAsErrors.
    private static Unit ContentWidth(Section section) =>
        section.PageSetup.PageWidth
        - section.PageSetup.LeftMargin
        - section.PageSetup.RightMargin;
}
