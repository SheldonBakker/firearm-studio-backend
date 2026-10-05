using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Products.GetPublicProducts;

public sealed class GetPublicProductsQueryHandler(
    IApplicationDbContext db,
    ITenantContext tenant,
    IFileStorage storage)
    : IQueryHandler<GetPublicProductsQuery, ErrorOr<PaginatedResponse<PublicProductResponse>>>
{
    public async Task<ErrorOr<PaginatedResponse<PublicProductResponse>>> Handle(
        GetPublicProductsQuery query, CancellationToken cancellationToken)
    {
        var company = await db.Companies
            .AsNoTracking()
            .Where(c => c.Id == query.CompanyId)
            .Select(c => new { c.IsActive, c.StorefrontKey })
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null || !company.IsActive || !StorefrontKey.Matches(company.StorefrontKey, query.Key))
        {
            return Error.NotFound(ErrorCodes.NotFound, "Company not found.");
        }

        using var scope = tenant.BeginCompanyScope(query.CompanyId);

        var ordered = ProductSort.ApplyPublic(
            db.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .ApplySearch(query.Search)
                .ApplyCategory(query.Category),
            query.SortBy,
            query.SortDir);

        var page = await ordered.ToPaginatedAsync(
            query.PageNumber, query.PageSize, ProductRow.QueryProjection, cancellationToken);

        var items = page.Items.Select(row => ProductResponseMapper.MapPublic(row, storage)).ToList();

        return new PaginatedResponse<PublicProductResponse>
        {
            Items = items,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetPublicProductsQuery.NotFound";
    }
}
