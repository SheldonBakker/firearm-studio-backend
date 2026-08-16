using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Invoices.GetInvoices;

public sealed class GetInvoicesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetInvoicesQuery, ErrorOr<PaginatedResponse<InvoiceListItemDto>>>
{
    public async Task<ErrorOr<PaginatedResponse<InvoiceListItemDto>>> Handle(
        GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.Invoices.AsNoTracking();

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(i => i.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.InvoiceNumber))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.InvoiceNumber.Trim());
            queryable = queryable.Where(i => EF.Functions.ILike(i.InvoiceNumber, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerName))
        {
            var pattern = SearchPatternHelper.ToILikeContainsPattern(query.CustomerName.Trim());
            queryable = queryable.Where(i =>
                (i.Customer!.FullName != null && EF.Functions.ILike(i.Customer.FullName, pattern)) ||
                (i.Customer!.CompanyName != null && EF.Functions.ILike(i.Customer.CompanyName, pattern)));
        }

        var desc = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        queryable = query.SortBy.ToLowerInvariant() switch
        {
            "total" => desc
                ? queryable.OrderByDescending(i => i.Total).ThenBy(i => i.Id)
                : queryable.OrderBy(i => i.Total).ThenBy(i => i.Id),
            "dueon" => desc
                ? queryable.OrderByDescending(i => i.DueOn).ThenBy(i => i.Id)
                : queryable.OrderBy(i => i.DueOn).ThenBy(i => i.Id),
            "invoicenumber" => desc
                ? queryable.OrderByDescending(i => i.InvoiceNumber).ThenBy(i => i.Id)
                : queryable.OrderBy(i => i.InvoiceNumber).ThenBy(i => i.Id),
            "status" => desc
                ? queryable.OrderByDescending(i => i.Status).ThenBy(i => i.Id)
                : queryable.OrderBy(i => i.Status).ThenBy(i => i.Id),
            _ => desc
                ? queryable.OrderByDescending(i => i.InvoiceMonth).ThenBy(i => i.Id)
                : queryable.OrderBy(i => i.InvoiceMonth).ThenBy(i => i.Id),
        };

        return await queryable.ToPaginatedAsync(
            query.PageNumber, query.PageSize, InvoiceListItemDto.QueryProjection, cancellationToken);
    }
}
