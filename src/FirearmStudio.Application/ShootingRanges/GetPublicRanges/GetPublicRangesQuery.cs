using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.ShootingRanges.GetPublicRanges;

public sealed record GetPublicRangesQuery(Guid CompanyId) : IQuery<ErrorOr<IReadOnlyList<PublicRangeResponse>>>;
