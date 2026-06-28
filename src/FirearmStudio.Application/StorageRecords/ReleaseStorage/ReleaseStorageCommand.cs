using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.StorageRecords.ReleaseStorage;

public sealed record ReleaseStorageCommand(Guid Id, ReleaseStorageRequest? Request) : ICommand<ErrorOr<Updated>>;
