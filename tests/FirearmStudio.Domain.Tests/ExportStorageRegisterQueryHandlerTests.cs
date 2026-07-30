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
        // The range guard runs first, so null dependencies prove no DB or
        // decryption work happens on the rejected path.
        var handler = new ExportStorageRegisterQueryHandler(null!, null!, null!, null!, null!);
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
}
