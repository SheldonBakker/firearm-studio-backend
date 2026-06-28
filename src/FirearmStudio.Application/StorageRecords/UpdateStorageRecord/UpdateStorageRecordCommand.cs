using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.StorageRecords.UpdateStorageRecord;

public sealed record UpdateStorageRecordCommand(Guid Id, UpdateStorageRecordRequest Request) : ICommand<ErrorOr<Updated>>;
