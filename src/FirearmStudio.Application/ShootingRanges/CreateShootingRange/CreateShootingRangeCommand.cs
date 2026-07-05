using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.ShootingRanges.CreateShootingRange;

public sealed record CreateShootingRangeCommand(CreateShootingRangeRequest Request) : ICommand<ErrorOr<ShootingRangeResponse>>;
