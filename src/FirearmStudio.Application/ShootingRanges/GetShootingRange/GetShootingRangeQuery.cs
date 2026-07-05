using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.ShootingRanges.GetShootingRange;

public sealed record GetShootingRangeQuery(Guid Id) : IQuery<ErrorOr<ShootingRangeResponse>>;
