using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Invoices.GetInvoice;

public sealed class GetInvoiceQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetInvoiceQuery, ErrorOr<InvoiceDetailDto>>
{
    public async Task<ErrorOr<InvoiceDetailDto>> Handle(GetInvoiceQuery query, CancellationToken cancellationToken)
    {
        return await db.Invoices
            .AsNoTracking()
            .Where(i => i.Id == query.Id)
            .FirstOrNotFoundAsync(InvoiceDetailDto.QueryProjection, ErrorCodes.NotFound, "Invoice not found.", cancellationToken);
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetInvoiceQuery.NotFound";
    }
}
