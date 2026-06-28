using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Firearms.GetFirearms;

public sealed record GetFirearmsQuery : IQuery<ErrorOr<IReadOnlyList<FirearmResponse>>>;
