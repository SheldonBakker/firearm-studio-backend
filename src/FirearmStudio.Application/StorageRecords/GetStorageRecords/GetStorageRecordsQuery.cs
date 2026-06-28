using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.StorageRecords.GetStorageRecords;

public sealed record GetStorageRecordsQuery(
    StorageStatus? StorageStatus,
    string? SerialNumber,
    string? CustomerName
) : IQuery<ErrorOr<IReadOnlyList<StorageRecordDto>>>;
