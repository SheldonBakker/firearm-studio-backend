using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Registers.ExportStorageRegister;

public sealed record ExportStorageRegisterQuery(
    RegisterKind Kind,
    DateOnly From,
    DateOnly To,
    RegisterExportFormat Format) : IQuery<ErrorOr<RegisterExportResult>>;

public sealed record RegisterExportResult(byte[] Content, string ContentType, string FileName);
