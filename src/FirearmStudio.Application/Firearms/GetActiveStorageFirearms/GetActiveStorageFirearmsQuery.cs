using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Firearms.GetActiveStorageFirearms;

public sealed record GetActiveStorageFirearmsQuery(
    string? SerialNumber,
    string? CustomerName,
    StorageStatus? StorageStatus
) : IQuery<ErrorOr<IReadOnlyList<ActiveStorageFirearmDto>>>;
