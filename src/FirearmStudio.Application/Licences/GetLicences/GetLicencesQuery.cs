using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Licences.GetLicences;

public sealed record GetLicencesQuery(
    string SortOrder,
    string? LicenceNumber,
    LicenceStatus? Status
) : IQuery<ErrorOr<IReadOnlyList<LicenceListItemDto>>>;
