using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.ShootingRanges.DeleteShootingRange;

public sealed record DeleteShootingRangeCommand(Guid Id) : ICommand<ErrorOr<Deleted>>;
