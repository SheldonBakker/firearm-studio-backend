using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;

namespace FirearmStudio.Application.Products.GetPublicProducts;

public sealed record GetPublicProductsQuery(
    Guid CompanyId,
    string? Key,
    int PageNumber,
    int PageSize,
    string? Search,
    string? Category,
    string SortBy,
    string SortDir) : IQuery<ErrorOr<PaginatedResponse<PublicProductResponse>>>;
