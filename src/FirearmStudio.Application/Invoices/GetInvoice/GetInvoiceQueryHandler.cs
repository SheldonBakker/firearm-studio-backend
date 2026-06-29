using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Invoices.GetInvoice;

public sealed class GetInvoiceQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetInvoiceQuery, ErrorOr<InvoiceDetailDto>>
{
    public async Task<ErrorOr<InvoiceDetailDto>> Handle(GetInvoiceQuery query, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .AsNoTracking()
            .Where(i => i.Id == query.Id)
            .Select(InvoiceDetailDto.QueryProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return invoice is null
            ? Error.NotFound(ErrorCodes.NotFound, "Invoice not found.")
            : invoice;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetInvoiceQuery.NotFound";
    }
}
