using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Licences.GetLicencesDueForRenewal;

public sealed record GetLicencesDueForRenewalQuery : IQuery<ErrorOr<IReadOnlyList<LicenceDueForRenewalDto>>>;
