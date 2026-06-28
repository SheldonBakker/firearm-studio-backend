using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Firearms.UpdateFirearm;

public sealed record UpdateFirearmCommand(Guid Id, UpdateFirearmRequest Request) : ICommand<ErrorOr<Updated>>;
