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

    private static (int PageCount, double WidthPt, double HeightPt) Inspect(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        return (pdf.PageCount, pdf.Pages[0].Width.Point, pdf.Pages[0].Height.Point);
    }

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
        var bytes = _renderer.Render(SampleDocument([["a", "b", "c", ""]]));

        var (_, width, height) = Inspect(bytes);

        Assert.Equal(841.89, width, 1);
        Assert.Equal(595.28, height, 1);
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

        Assert.Equal(2, Inspect(bytes).PageCount);

        var text = ExtractContentStreamText(bytes);
        Assert.Contains("(Column) Tj", text);
        Assert.Contains("(15) Tj", text);
    }

    [Fact]
    public void Render_tolerates_a_document_with_no_columns()
    {
        var bytes = _renderer.Render(SampleDocument([], columns: []));

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Render_tolerates_rows_shorter_and_longer_than_the_column_count()
    {
        var bytes = _renderer.Render(SampleDocument(
        [
            ["only-one"],
            ["b", "f", "h", "j", "k", "m"],
        ]));

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));

        var text = ExtractContentStreamText(bytes);

        var onlyOneCharacterTokens = new[]
        {
            "(o) Tj", "(n) Tj", "(l) Tj", "(y) Tj", "(-) Tj", "(o) Tj", "(n) Tj", "(e) Tj",
        };
        var searchFrom = 0;
        foreach (var token in onlyOneCharacterTokens)
        {
            var index = text.IndexOf(token, searchFrom, StringComparison.Ordinal);
            Assert.True(index >= 0, $"Expected to find '{token}' at or after position {searchFrom}.");
            searchFrom = index + token.Length;
        }

        Assert.Contains("(j) Tj", text);

        Assert.DoesNotContain("(k) Tj", text);
        Assert.DoesNotContain("(m) Tj", text);
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
        var document = SampleDocument(
            Enumerable.Range(0, 50)
                .Select(i => new[] { $"2026-02-{(i % 28) + 1:00}", "CZ", $"SN{i:0000}", "" })
                .ToList());

        var baselineLength = _renderer.Render(document).Length;

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
        var rows = Enumerable.Range(0, 25)
            .Select(i => new[] { $"2026-02-{(i % 28) + 1:00}", "CZ", $"SN{i:0000}", "" })
            .ToList();

        var bytes = _renderer.Render(SampleDocument(rows));

        Assert.Equal(2, Inspect(bytes).PageCount);
    }

    [Fact]
    public void Render_stamps_the_creation_date_as_a_true_utc_instant()
    {
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
