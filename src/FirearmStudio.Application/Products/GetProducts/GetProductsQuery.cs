using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;

namespace FirearmStudio.Application.Products.GetProducts;

public sealed record GetProductsQuery(
    int PageNumber,
    int PageSize,
    string? Search,
    string? Category,
    bool? IsActive,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinStock,
    int? MaxStock,
    string SortBy,
    string SortDir) : IQuery<ErrorOr<PaginatedResponse<ProductResponse>>>;
