using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Registers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FirearmStudio.Infrastructure.Services;

public sealed class QuestPdfRegisterRenderer : IRegisterPdfRenderer
{
    static QuestPdfRegisterRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(RegisterDocument document) =>
        Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(24);
            page.DefaultTextStyle(style => style.FontSize(8));

            page.Header().Column(header =>
            {
                header.Item().Text(document.CompanyName).FontSize(14).Bold();

                if (!string.IsNullOrWhiteSpace(document.CompanyRegistrationNumber))
                {
                    header.Item().Text($"Registration No: {document.CompanyRegistrationNumber}");
                }

                if (!string.IsNullOrWhiteSpace(document.CompanyAddress))
                {
                    header.Item().Text(document.CompanyAddress);
                }

                header.Item().PaddingTop(6).Text(document.Title).FontSize(12).Bold();
                header.Item().Text($"Period: {document.PeriodFrom:yyyy-MM-dd} to {document.PeriodTo:yyyy-MM-dd}");
                header.Item().PaddingBottom(8).Text(
                    $"Generated {document.GeneratedAt:yyyy-MM-dd HH:mm} (SAST) by {document.GeneratedBy}");
            });

            page.Content().Element(content => ComposeTable(content, document));

            page.Footer().Row(row =>
            {
                row.RelativeItem().Text($"Total rows: {document.Rows.Count}");
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        })).GeneratePdf();

    private static void ComposeTable(IContainer container, RegisterDocument document)
    {
        if (document.Rows.Count == 0)
        {
            container.PaddingTop(12).Text(document.EmptyStateText).Italic();
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                for (var i = 0; i < document.Columns.Count; i++)
                {
                    columns.RelativeColumn(document.ColumnWeights?[i] ?? 1f);
                }
            });

            table.Header(header =>
            {
                foreach (var column in document.Columns)
                {
                    header.Cell()
                        .Border(0.5f)
                        .Background(Colors.Grey.Lighten3)
                        .Padding(3)
                        .Text(column)
                        .Bold();
                }
            });

            foreach (var row in document.Rows)
            {
                foreach (var cell in row)
                {
                    table.Cell().Border(0.5f).Padding(3).Text(cell);
                }
            }
        });
    }
}
