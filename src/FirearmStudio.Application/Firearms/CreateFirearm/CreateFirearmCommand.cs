using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Firearms.CreateFirearm;

public sealed record CreateFirearmCommand(CreateFirearmRequest Request) : ICommand<ErrorOr<FirearmResponse>>;
