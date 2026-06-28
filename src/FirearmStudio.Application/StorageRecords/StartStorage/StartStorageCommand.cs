using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.StorageRecords.StartStorage;

public sealed record StartStorageCommand(Guid FirearmId, StartStorageRequest Request) : ICommand<ErrorOr<Guid>>;
