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

    // MigraDoc does NOT reserve body space for a page header/footer the way QuestPDF's
    // page.Header()/page.Footer() do - HeaderDistance/FooterDistance only place the header and
    // footer frames relative to the page edge, and TopMargin/BottomMargin only place the body.
    // Left alone, the body overlaps the header and footer on every page.
    //
    // These constants were derived by rendering the worst-case six-line header (both optional
    // "Registration No" and address lines present) and reading the exact glyph baselines back out
    // of the generated PDF's content stream, then adding the Roboto font's own ascent/descent
    // (via PdfSharp's XFont metrics) and the 8pt header/table gap called for by the layout spec:
    //   ascent(Bold 14) 14.67pt + line-to-line offsets 65.66pt + descent(Regular 8) 2.17pt
    //   + 8pt gap = ~90.5pt of header content below HeaderDistance, rounded up to 91pt.
    // The footer is a single Regular-8 line: ascent 8.38pt + descent 2.17pt + 8pt gap = ~18.55pt,
    // rounded up to 19pt.
    private const double HeaderDistancePoints = MarginPoints;  // keeps the same 24pt visual top gap
    private const double FooterDistancePoints = MarginPoints;  // keeps the same 24pt visual bottom gap
    private const double HeaderReservedHeightPoints = 91;
    private const double FooterReservedHeightPoints = 19;
    private const double TopMarginPoints = HeaderDistancePoints + HeaderReservedHeightPoints;
    private const double BottomMarginPoints = FooterDistancePoints + FooterReservedHeightPoints;

    private static readonly Color HeaderFill = new(238, 238, 238);

    // PDFsharp's global font cache is not thread-safe. A Render call left unsynchronised can
    // interleave with another on a different thread mid-glyph-lookup: measured at 32-way
    // parallelism this silently injected a spurious bold font switch into a data cell roughly 3%
    // of the time (12/384 documents), with no exception raised - a corrupted, unsigned-looking
    // compliance document. A lock around the whole compose-plus-render-plus-save sequence
    // eliminated the deviation entirely (0/384). The cost is that PDF exports now serialise
    // process-wide, which is acceptable: the largest register (5000 rows) renders in well under 4
    // seconds, and register exports are infrequent admin operations, not a hot request path.
    private static readonly Lock RenderGate = new();

    static PdfSharpRegisterRenderer()
    {
        // PDFsharp allows the font resolver to be set exactly once per process, and never after
        // the first XFont exists. A static constructor is the only place that holds for a
        // singleton service resolved from concurrent request threads.
        GlobalFontSettings.FontResolver = new EmbeddedFontResolver();
    }

    public byte[] Render(RegisterDocument document)
    {
        lock (RenderGate)
        {
            var pdf = new PdfDocumentRenderer { Document = Compose(document) };
            pdf.RenderDocument();

            // Pinning the PDF's timestamps to the register's own GeneratedAt is a desirable
            // property for an audit document - the export's metadata records when the register
            // content was generated, not when the bytes happened to be written - and it also
            // makes same-document renders reproducible aside from PdfSharp's internal, read-only
            // per-document Guid (used for the trailer /ID and XMP DocumentID/InstanceID), which
            // cannot be pinned.
            pdf.PdfDocument.Info.CreationDate = document.GeneratedAt;
            pdf.PdfDocument.Info.ModificationDate = document.GeneratedAt;

            using var stream = new MemoryStream();
            pdf.PdfDocument.Save(stream, false);
            return stream.ToArray();
        }
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

        // TopMargin/BottomMargin must clear the header/footer frames explicitly - see the
        // constants' comment above for how these numbers were measured.
        section.PageSetup.HeaderDistance = Unit.FromPoint(HeaderDistancePoints);
        section.PageSetup.FooterDistance = Unit.FromPoint(FooterDistancePoints);
        section.PageSetup.TopMargin = Unit.FromPoint(TopMarginPoints);
        section.PageSetup.BottomMargin = Unit.FromPoint(BottomMarginPoints);

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

        header.AddParagraph(RegisterCellText.Sanitise(
            $"Period: {source.PeriodFrom:yyyy-MM-dd} to {source.PeriodTo:yyyy-MM-dd}"));

        var generated = header.AddParagraph(RegisterCellText.Sanitise(
            $"Generated {source.GeneratedAt:yyyy-MM-dd HH:mm} (SAST) by {source.GeneratedBy}"));
        generated.Format.SpaceAfter = Unit.FromPoint(8);
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

        // Without this, the table's row left indent interacts with the column LeftIndent used for
        // cell padding and the whole table drifts a few points left of the page's LeftMargin.
        table.Rows.LeftIndent = Unit.Zero;

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
