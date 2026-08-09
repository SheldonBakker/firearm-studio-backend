using System.Diagnostics;
using FirearmStudio.Application.Registers;
using FirearmStudio.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace FirearmStudio.Infrastructure.Tests;

public class PdfSharpRegisterRendererPerformanceTests(ITestOutputHelper output)
{
    // ExportStorageRegisterQueryHandler caps PDF exports at 5000 rows and renders them
    // synchronously inside the HTTP request, so this is a product requirement, not a benchmark.
    private const int MaxPdfExportRows = 5000;
    private const int BudgetSeconds = 10;

    [Fact]
    public void Rendering_the_maximum_export_stays_within_the_request_budget()
    {
        var columns = Enumerable.Range(0, 16).Select(i => $"Column {i}").ToList();
        float[] weights = [0.9f, 0.9f, 0.9f, 0.9f, 0.8f, 1.1f, 1.3f, 1.2f, 1.8f, 1.2f, 0.9f, 0.8f, 0.8f, 1.2f, 0.9f, 1.0f];

        var rows = Enumerable.Range(0, MaxPdfExportRows)
            .Select(i => Enumerable.Range(0, 16).Select(c => $"r{i}c{c}").ToArray())
            .ToList();

        var document = new RegisterDocument(
            "Safe Custody Register", "Bergview Arms", "2015/098765/07",
            "12 Range Rd, Paarl, Western Cape, 7646",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30),
            new DateTime(2026, 7, 29, 12, 0, 0), "sheldon@wbwr.io",
            columns, rows, "No movements in period.", weights);

        var renderer = new PdfSharpRegisterRenderer();

        var sw = Stopwatch.StartNew();
        var bytes = renderer.Render(document);
        sw.Stop();

        output.WriteLine($"{MaxPdfExportRows} rows x 16 columns: {sw.ElapsedMilliseconds} ms, {bytes.Length} bytes");

        Assert.True(bytes.Length > 10_000);
        Assert.True(
            sw.Elapsed.TotalSeconds < BudgetSeconds,
            $"Rendering {MaxPdfExportRows} rows took {sw.Elapsed.TotalSeconds:F1}s, over the {BudgetSeconds}s budget.");
    }
}
