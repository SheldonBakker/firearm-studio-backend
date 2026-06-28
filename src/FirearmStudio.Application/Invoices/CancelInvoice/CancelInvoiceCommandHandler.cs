using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Invoices.CancelInvoice;

public sealed class CancelInvoiceCommandHandler(IApplicationDbContext db)
    : ICommandHandler<CancelInvoiceCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(CancelInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);
        if (invoice is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Invoice not found.");
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return Error.Conflict(ErrorCodes.AlreadyPaid, "Cannot cancel a paid invoice.");
        }

        invoice.Status = InvoiceStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "CancelInvoiceCommand.NotFound";
        public const string AlreadyPaid = "CancelInvoiceCommand.AlreadyPaid";
    }
}
