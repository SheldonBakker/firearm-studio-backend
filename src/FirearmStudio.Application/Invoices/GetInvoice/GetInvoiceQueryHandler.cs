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
            .Select(i => new InvoiceDetailDto(
                i.Id,
                i.CustomerId,
                i.InvoiceNumber,
                i.InvoiceMonth,
                i.Subtotal,
                i.VatAmount,
                i.Total,
                i.Status,
                i.SentAt,
                i.DueOn,
                i.Lines.Select(l => new InvoiceLineDto(l.Id, l.Description, l.Quantity, l.UnitPrice, l.LineTotal)).ToList(),
                i.Payments.Select(p => new InvoicePaymentDto(p.Id, p.Amount, p.PaidOn, p.Method, p.Reference)).ToList()))
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
