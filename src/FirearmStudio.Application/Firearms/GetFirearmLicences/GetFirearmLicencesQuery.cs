using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Firearms.GetFirearmLicences;

public sealed record GetFirearmLicencesQuery(Guid FirearmId)
    : IQuery<ErrorOr<IReadOnlyList<FirearmLicenceListItemDto>>>;
