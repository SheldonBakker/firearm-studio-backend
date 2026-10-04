using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Products.GetProduct;

public sealed class GetProductQueryHandler(IApplicationDbContext db, IFileStorage storage)
    : IQueryHandler<GetProductQuery, ErrorOr<ProductResponse>>
{
    public async Task<ErrorOr<ProductResponse>> Handle(GetProductQuery query, CancellationToken cancellationToken)
    {
        var row = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == query.Id)
            .FirstOrNotFoundAsync(ProductRow.QueryProjection, ErrorCodes.NotFound, "Product not found.", cancellationToken);

        if (row.IsError)
        {
            return row.Errors;
        }

        return ProductResponseMapper.Map(row.Value, storage);
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetProductQuery.NotFound";
    }
}
