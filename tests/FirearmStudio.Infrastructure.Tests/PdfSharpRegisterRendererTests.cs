using System.Text;
using FirearmStudio.Application.Registers;
using FirearmStudio.Infrastructure.Services;
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
        Assert.True(Inspect(bytes).PageCount >= 1);
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
    }

    [Fact]
    public void Render_tolerates_control_characters_and_missing_company_details()
    {
        var document = SampleDocument([["a\nb", "c\td", "e", ""]]) with
        {
            CompanyRegistrationNumber = null,
            CompanyAddress = string.Empty,
        };

        var bytes = _renderer.Render(document);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Render_is_safe_to_call_concurrently()
    {
        // Registered as a singleton and called from parallel request threads.
        var results = new byte[8][];

        Parallel.For(0, 8, i =>
        {
            results[i] = _renderer.Render(SampleDocument([[$"row{i}", "CZ", "SN1", ""]]));
        });

        Assert.All(results, bytes =>
        {
            Assert.NotNull(bytes);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        });
    }
}
