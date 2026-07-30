using System.Text;
using FirearmStudio.Application.Registers;
using FirearmStudio.Infrastructure.Services;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public class QuestPdfRegisterRendererTests
{
    private static RegisterDocument SampleDocument(IReadOnlyList<string[]> rows) => new(
        Title: "Safe Custody Register",
        CompanyName: "Bergview Arms",
        CompanyRegistrationNumber: "2015/098765/07",
        CompanyAddress: "12 Range Rd, Paarl, Western Cape, 7646",
        PeriodFrom: new DateOnly(2026, 1, 1),
        PeriodTo: new DateOnly(2026, 6, 30),
        GeneratedAt: new DateTime(2026, 7, 29, 12, 0, 0),
        GeneratedBy: "sheldon@wbwr.io",
        Columns: ["Date Received", "Make", "Serial Number", "Signature"],
        Rows: rows,
        EmptyStateText: "No movements in period.");

    [Fact]
    public void Render_produces_a_pdf_file()
    {
        var bytes = new QuestPdfRegisterRenderer().Render(
            SampleDocument([["2026-02-01", "CZ", "SN123", ""], ["2026-03-05", "Glock", "SN456", ""]]));

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Render_with_no_rows_still_produces_a_pdf_file()
    {
        var bytes = new QuestPdfRegisterRenderer().Render(SampleDocument([]));

        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
