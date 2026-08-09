using System.Text;
using FirearmStudio.Application.Registers;
using FirearmStudio.Infrastructure.Services;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public class PdfSharpRegisterRendererTests
{
    private readonly PdfSharpRegisterRenderer _renderer = new();

    private static RegisterDocument SampleDocument(
        IReadOnlyList<string[]> rows,
        IReadOnlyList<string>? columns = null,
        IReadOnlyList<float>? weights = null) => new(
        Title: "Safe Custody Register",
        CompanyName: "Bergview Arms",
        CompanyRegistrationNumber: "2015/098765/07",
        CompanyAddress: "12 Range Rd, Paarl, Western Cape, 7646",
        PeriodFrom: new DateOnly(2026, 1, 1),
        PeriodTo: new DateOnly(2026, 6, 30),
        GeneratedAt: new DateTime(2026, 7, 29, 12, 0, 0),
        GeneratedBy: "sheldon@wbwr.io",
        Columns: columns ?? ["Date Received", "Make", "Serial Number", "Signature"],
        Rows: rows,
        EmptyStateText: "No movements in period.",
        ColumnWeights: weights);

    // ReadOnly and InformationOnly are both [Obsolete] in 6.2.4; Import is the supported mode
    // and exposes PageCount and page dimensions.
    private static (int PageCount, double WidthPt, double HeightPt) Inspect(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        return (pdf.PageCount, pdf.Pages[0].Width.Point, pdf.Pages[0].Height.Point);
    }

    // Content streams are FlateDecode-compressed by default; PdfContent.Stream.UnfilteredValue
    // decodes them so a test can assert on the actual PDF text-show ("Tj") operators rather than
    // just "it produced some bytes".
    private static string ExtractContentStreamText(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

        var builder = new StringBuilder();
        for (var i = 0; i < pdf.PageCount; i++)
        {
            foreach (PdfContent content in pdf.Pages[i].Contents)
            {
                builder.Append(Encoding.Latin1.GetString(content.Stream.UnfilteredValue));
            }
        }

        return builder.ToString();
    }

    [Fact]
    public void Render_produces_a_pdf_file()
    {
        var bytes = _renderer.Render(
            SampleDocument([["2026-02-01", "CZ", "SN123", ""], ["2026-03-05", "Glock", "SN456", ""]]));

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Render_with_no_rows_still_produces_a_pdf_file()
    {
        var bytes = _renderer.Render(SampleDocument([]));

        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.Equal(1, Inspect(bytes).PageCount);
    }

    [Fact]
    public void Pages_are_A4_landscape()
    {
        // MigraDoc's PageSetup.PageWidth reports the HEIGHT in landscape; this asserts the
        // rendered page box, not the DOM, so an upstream change to that behaviour is caught here.
        var bytes = _renderer.Render(SampleDocument([["a", "b", "c", ""]]));

        var (_, width, height) = Inspect(bytes);

        Assert.Equal(841.89, width, 1);   // 297 mm
        Assert.Equal(595.28, height, 1);  // 210 mm
        Assert.True(width > height, "Register pages must be landscape.");
    }

    [Fact]
    public void A_long_register_paginates()
    {
        var rows = Enumerable.Range(0, 200)
            .Select(i => new[] { $"2026-02-{(i % 28) + 1:00}", "CZ", $"SN{i:0000}", "" })
            .ToList();

        var bytes = _renderer.Render(SampleDocument(rows));

        Assert.True(Inspect(bytes).PageCount > 1, "200 rows should span more than one page.");
    }

    [Fact]
    public void Render_handles_the_full_sixteen_column_safe_custody_shape()
    {
        var columns = Enumerable.Range(0, 16).Select(i => $"Column {i}").ToList();
        float[] weights = [0.9f, 0.9f, 0.9f, 0.9f, 0.8f, 1.1f, 1.3f, 1.2f, 1.8f, 1.2f, 0.9f, 0.8f, 0.8f, 1.2f, 0.9f, 1.0f];
        var rows = Enumerable.Range(0, 40)
            .Select(i => Enumerable.Range(0, 16).Select(c => $"r{i}c{c}").ToArray())
            .ToList();

        var bytes = _renderer.Render(SampleDocument(rows, columns, weights));

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));

        // 40 rows at the same 8pt row height as every other case in this suite fits 24 rows per
        // page under the reserved header/footer space, so this must land on exactly two pages.
        Assert.Equal(2, Inspect(bytes).PageCount);

        // MigraDoc splits text into a "Tj" run per word, so "Column 15" shows up as two separate
        // show-text operators rather than one. Their presence confirms the 16th column header
        // actually reached the table instead of being silently dropped by the width calculation.
        var text = ExtractContentStreamText(bytes);
        Assert.Contains("(Column) Tj", text);
        Assert.Contains("(15) Tj", text);
    }

    [Fact]
    public void Render_tolerates_a_document_with_no_columns()
    {
        // MigraDoc throws on a table with zero columns, so the renderer must fall back
        // to the empty state rather than crash the export.
        var bytes = _renderer.Render(SampleDocument([], columns: []));

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Render_tolerates_rows_shorter_and_longer_than_the_column_count()
    {
        var bytes = _renderer.Render(SampleDocument(
        [
            ["only-one"],
            ["a", "b", "c", "d", "e", "f"],
        ]));

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));

        var text = ExtractContentStreamText(bytes);

        // The four-column default has room for "only-one" (its own row's remaining three cells
        // render blank, which shows up as nothing to assert on rather than a token) and for the
        // first four values of the six-value row. MigraDoc line-wraps at the hyphen, so
        // "only-one" is two show-text operators, not one.
        Assert.Contains("(only-)", text);
        Assert.Contains("(one) Tj", text);
        Assert.Contains("(d) Tj", text);

        // The fifth and sixth values of the long row must never reach the page.
        Assert.DoesNotContain("(e) Tj", text);
        Assert.DoesNotContain("(f) Tj", text);
    }

    [Fact]
    public void Render_tolerates_control_characters_and_missing_company_details()
    {
        var withControlCharacters = SampleDocument([["a\nb", "c\td", "e", ""]]) with
        {
            CompanyRegistrationNumber = null,
            CompanyAddress = string.Empty,
        };

        var bytes = _renderer.Render(withControlCharacters);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));

        // MigraDoc does not throw on a raw newline or tab inside a paragraph - it turns them into
        // layout, which silently changes row heights and pagination instead of raising an error.
        // RegisterCellText.Sanitise collapses "a\nb" and "c\td" to "a b" and "c d" before they
        // reach MigraDoc, so a document built from the pre-collapsed strings must render
        // byte-length identical to one built from the raw control characters. If Sanitise were
        // bypassed, the raw control characters would add layout the equivalent-spaces document
        // does not have, and the lengths would diverge.
        var withEquivalentSpaces = SampleDocument([["a b", "c d", "e", ""]]) with
        {
            CompanyRegistrationNumber = null,
            CompanyAddress = string.Empty,
        };

        var equivalentBytes = _renderer.Render(withEquivalentSpaces);

        Assert.Equal(equivalentBytes.Length, bytes.Length);
    }

    [Fact]
    public void Render_is_safe_to_call_concurrently()
    {
        // Registered as a singleton and called from parallel request threads. PDFsharp's global
        // font cache is not thread-safe, so an unsynchronised Render can corrupt a glyph under
        // contention without throwing - a passing "%PDF" + non-null check would not catch that.
        //
        // PdfDocument.Info.CreationDate/ModificationDate are pinned from GeneratedAt, so repeated
        // renders of the same document agree there, but PdfSharp's internal per-document Guid
        // (used for the trailer /ID and XMP DocumentID/InstanceID) is regenerated on every render
        // and has no public setter, so byte-for-byte equality across renders is not achievable.
        // Byte length is: the glyph corruption this guards against injects an extra font-switch
        // operator into the content stream, which changes the length.
        var document = SampleDocument(
            Enumerable.Range(0, 50)
                .Select(i => new[] { $"2026-02-{(i % 28) + 1:00}", "CZ", $"SN{i:0000}", "" })
                .ToList());

        var baselineLength = _renderer.Render(document).Length; // warm the font pipeline serially

        var results = new byte[32][];

        Parallel.For(0, 32, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
        {
            results[i] = _renderer.Render(document);
        });

        Assert.All(results, bytes =>
        {
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
            Assert.Equal(baselineLength, bytes.Length);
        });
    }

    [Fact]
    public void Reserved_header_and_footer_space_yields_the_expected_page_count()
    {
        // Pins the exact page count for a fixed row count so a regression in the reserved
        // header/footer space - the TopMargin/BottomMargin constants that keep the table clear of
        // the header and footer - shows up as a failing assertion instead of a silently corrupted
        // layout. 24 rows fit on a single page under the current reservation; one more does not,
        // so 25 rows is the smallest row count that must span two pages.
        var rows = Enumerable.Range(0, 25)
            .Select(i => new[] { $"2026-02-{(i % 28) + 1:00}", "CZ", $"SN{i:0000}", "" })
            .ToList();

        var bytes = _renderer.Render(SampleDocument(rows));

        Assert.Equal(2, Inspect(bytes).PageCount);
    }

    [Fact]
    public void Render_stamps_the_creation_date_as_a_true_utc_instant()
    {
        // SampleDocument's GeneratedAt is 2026-07-29 12:00:00, which RegisterDocumentFactory
        // produces as SAST wall-clock time (South Africa has no DST, a constant UTC+2), so the
        // true UTC instant is 2026-07-29 10:00:00. This expected value is hardcoded rather than
        // recomputed via TimeZoneInfo/SouthAfricaTimeZone, the same conversion the renderer itself
        // performs, so a bug in that conversion cannot cancel out between production code and test.
        //
        // This must hold regardless of the host's own time zone - PdfSharp derives the PDF's
        // /CreationDate offset from the DateTime's Kind, not from the host clock, once the
        // renderer hands it a true DateTimeKind.Utc value. The CI/production run of this test does
        // not control TZ, so a value that only matched under the developer's local offset would be
        // a false negative for what actually shipped.
        var expectedUtc = new DateTime(2026, 7, 29, 10, 0, 0, DateTimeKind.Utc);

        var bytes = _renderer.Render(SampleDocument([["2026-02-01", "CZ", "SN0000", ""]]));

        using var stream = new MemoryStream(bytes);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

        Assert.Equal(DateTimeKind.Utc, pdf.Info.CreationDate.Kind);
        Assert.Equal(expectedUtc, pdf.Info.CreationDate);
        Assert.Equal(DateTimeKind.Utc, pdf.Info.ModificationDate.Kind);
        Assert.Equal(expectedUtc, pdf.Info.ModificationDate);
    }
}
