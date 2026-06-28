using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Licences.GetExpiredLicences;

public sealed record GetExpiredLicencesQuery : IQuery<ErrorOr<IReadOnlyList<ExpiredLicenceDto>>>;
