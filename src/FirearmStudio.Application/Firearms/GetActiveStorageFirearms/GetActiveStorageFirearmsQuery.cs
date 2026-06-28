using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Firearms.GetActiveStorageFirearms;

public sealed record GetActiveStorageFirearmsQuery : IQuery<ErrorOr<IReadOnlyList<ActiveStorageFirearmDto>>>;
