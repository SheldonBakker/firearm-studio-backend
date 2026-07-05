using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;

namespace FirearmStudio.Application.ShootingRanges.GetShootingRanges;

public sealed record GetShootingRangesQuery(
    int PageNumber,
    int PageSize,
    string SortOrder,
    string? Name,
    bool? IsActive) : IQuery<ErrorOr<PaginatedResponse<ShootingRangeListItemDto>>>;
