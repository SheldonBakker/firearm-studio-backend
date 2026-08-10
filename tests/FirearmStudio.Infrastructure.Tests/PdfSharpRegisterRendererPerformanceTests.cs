using System.Diagnostics;
using FirearmStudio.Application.Registers;
using FirearmStudio.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace FirearmStudio.Infrastructure.Tests;

public class PdfSharpRegisterRendererPerformanceTests(ITestOutputHelper output)
{
    private const int MaxPdfExportRows = 2000;

    private const int BudgetSeconds = 10;

    private static readonly IReadOnlyList<string> Columns =
    [
        "Date Received", "Date Disposed", "Make", "Model", "Type", "Calibre",
        "Serial Number", "Licence Number", "Owner Name", "ID Number",
        "Address", "Purpose", "Condition", "Received From", "Remarks", "Signature",
    ];

    private static readonly float[] Weights =
        [0.9f, 0.9f, 0.9f, 0.9f, 0.8f, 1.1f, 1.3f, 1.2f, 1.8f, 1.2f, 0.9f, 0.8f, 0.8f, 1.2f, 0.9f, 1.0f];

    private static string[] RealisticRow(int i) =>
    [
        $"2026-0{(i % 6) + 1}-01",
        $"2026-0{(i % 6) + 1}-15",
        "Colt",
        "Government",
        "Handgun",
        ".45 ACP",
        $"SN{i:00000}X{i % 7}",
        $"WC/2020/{i:00000}",
        "Christiaan van der Merwe",
        $"85{i % 10}1015800{i % 100:00}",
        "12 Range Road, Muizenberg, Cape Town, Western Cape, 7945",
        "Safe custody storage",
        "Serviceable",
        "Bergview Arms Dealer CC",
        "No irregularities noted at intake, firearm inspected and logged accordingly",
        "",
    ];

    [Fact]
    public void Rendering_the_maximum_export_stays_within_the_request_budget()
    {
        var rows = Enumerable.Range(0, MaxPdfExportRows)
            .Select(RealisticRow)
            .ToList();

        var document = new RegisterDocument(
            "Safe Custody Register", "Bergview Arms", "2015/098765/07",
            "12 Range Rd, Paarl, Western Cape, 7646",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30),
            new DateTime(2026, 7, 29, 12, 0, 0), "sheldon@wbwr.io",
            Columns, rows, "No movements in period.", Weights);

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
