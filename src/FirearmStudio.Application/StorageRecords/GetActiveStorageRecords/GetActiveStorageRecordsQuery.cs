using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.StorageRecords.GetActiveStorageRecords;

public sealed record GetActiveStorageRecordsQuery : IQuery<ErrorOr<IReadOnlyList<ActiveStorageRecordDto>>>;
