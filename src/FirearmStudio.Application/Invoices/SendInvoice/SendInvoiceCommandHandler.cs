using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Invoices.SendInvoice;

public sealed class SendInvoiceCommandHandler(IApplicationDbContext db)
    : ICommandHandler<SendInvoiceCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(SendInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);
        if (invoice is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Invoice not found.");
        }

        if (invoice.Status is InvoiceStatus.Cancelled or InvoiceStatus.Paid)
        {
            return Error.Conflict(ErrorCodes.InvalidStatus, $"Cannot send an invoice that is {invoice.Status}.");
        }

        invoice.Status = InvoiceStatus.Sent;
        invoice.SentAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "SendInvoiceCommand.NotFound";
        public const string InvalidStatus = "SendInvoiceCommand.InvalidStatus";
    }
}
