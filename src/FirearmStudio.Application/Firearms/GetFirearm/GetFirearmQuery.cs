using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Firearms.GetFirearm;

public sealed record GetFirearmQuery(Guid Id) : IQuery<ErrorOr<FirearmResponse>>;
