using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Licences.GetLicence;

public sealed record GetLicenceQuery(Guid Id) : IQuery<ErrorOr<LicenceDetailDto>>;
