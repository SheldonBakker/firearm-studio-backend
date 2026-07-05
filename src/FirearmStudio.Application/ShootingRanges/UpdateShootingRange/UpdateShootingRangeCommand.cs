using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.ShootingRanges.UpdateShootingRange;

public sealed record UpdateShootingRangeCommand(Guid Id, UpdateShootingRangeRequest Request) : ICommand<ErrorOr<Updated>>;
