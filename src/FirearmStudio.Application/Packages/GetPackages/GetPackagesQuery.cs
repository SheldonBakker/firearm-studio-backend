using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;

namespace FirearmStudio.Application.Packages.GetPackages;

public sealed record GetPackagesQuery(
    int PageNumber,
    int PageSize,
    string SortBy,
    string SortOrder,
    string? Name,
    bool? IsActive) : IQuery<ErrorOr<PaginatedResponse<PackageListItemDto>>>;
