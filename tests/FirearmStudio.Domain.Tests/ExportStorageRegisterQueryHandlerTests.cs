using ErrorOr;
using FirearmStudio.Application.Registers;
using FirearmStudio.Application.Registers.ExportStorageRegister;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class ExportStorageRegisterQueryHandlerTests
{
    [Fact]
    public async Task Handle_rejects_from_after_to_before_touching_any_dependency()
    {
        var handler = new ExportStorageRegisterQueryHandler(null!, null!, null!, null!);
        var query = new ExportStorageRegisterQuery(
            RegisterKind.Firearms,
            new DateOnly(2026, 6, 30),
            new DateOnly(2026, 1, 1),
            RegisterExportFormat.Csv);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ExportStorageRegisterQueryHandler.ErrorCodes.InvalidRange, result.FirstError.Code);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Theory]
    [InlineData(RegisterExportFormat.Pdf, ExportStorageRegisterQueryHandler.MaxPdfExportRows)]
    [InlineData(RegisterExportFormat.Csv, ExportStorageRegisterQueryHandler.MaxExportRows)]
    public void A_row_count_at_the_cap_is_accepted(RegisterExportFormat format, int cap)
    {
        Assert.Null(ExportStorageRegisterQueryHandler.RowCapError(format, cap));
    }

    [Fact]
    public void A_pdf_export_above_two_thousand_rows_is_rejected()
    {
        var error = ExportStorageRegisterQueryHandler.RowCapError(
            RegisterExportFormat.Pdf, ExportStorageRegisterQueryHandler.MaxPdfExportRows + 1);

        Assert.NotNull(error);
        Assert.Equal(ExportStorageRegisterQueryHandler.ErrorCodes.TooManyRows, error.Value.Code);
        Assert.Equal(ErrorType.Validation, error.Value.Type);
        Assert.Contains(
            $"PDF register export is limited to {ExportStorageRegisterQueryHandler.MaxPdfExportRows} rows",
            error.Value.Description,
            StringComparison.Ordinal);
        Assert.Contains("export CSV for wider ranges", error.Value.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void A_csv_export_above_twenty_thousand_rows_is_rejected()
    {
        var error = ExportStorageRegisterQueryHandler.RowCapError(
            RegisterExportFormat.Csv, ExportStorageRegisterQueryHandler.MaxExportRows + 1);

        Assert.NotNull(error);
        Assert.Equal(ExportStorageRegisterQueryHandler.ErrorCodes.TooManyRows, error.Value.Code);
        Assert.Equal(ErrorType.Validation, error.Value.Type);
        Assert.Contains(
            $"CSV register export is limited to {ExportStorageRegisterQueryHandler.MaxExportRows} rows",
            error.Value.Description,
            StringComparison.Ordinal);
        Assert.DoesNotContain("export CSV", error.Value.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void The_pdf_cap_is_ten_times_smaller_than_the_csv_cap()
    {
        Assert.Equal(2000, ExportStorageRegisterQueryHandler.MaxPdfExportRows);
        Assert.Equal(20000, ExportStorageRegisterQueryHandler.MaxExportRows);
    }
}
