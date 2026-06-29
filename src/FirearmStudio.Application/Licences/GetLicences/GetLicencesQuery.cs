using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Licences.GetLicences;

public sealed record GetLicencesQuery(
    int PageNumber,
    int PageSize,
    string SortOrder,
    string? LicenceNumber,
    LicenceStatus? Status
) : IQuery<ErrorOr<PaginatedResponse<LicenceListItemDto>>>;
