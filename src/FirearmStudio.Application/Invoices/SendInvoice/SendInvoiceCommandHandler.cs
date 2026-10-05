using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Email;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Invoices.SendInvoice;

public sealed class SendInvoiceCommandHandler(
    IApplicationDbContext db,
    ITransactionalEmailSender emailSender,
    ILogger<SendInvoiceCommandHandler> logger)
    : ICommandHandler<SendInvoiceCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(SendInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);
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

        await SendEmailAsync(invoice, cancellationToken);

        return Result.Updated;
    }

    private async Task SendEmailAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var email = invoice.Customer?.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning(
                "Skipped invoice-sent email for invoice {InvoiceNumber}: customer has no email.",
                invoice.InvoiceNumber);
            return;
        }

        try
        {
            var company = await db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == invoice.CompanyId, cancellationToken);

            var companyDetails = company is not null
                ? CompanyEmailDetails.From(company)
                : new CompanyEmailDetails(null, null, null, null, null, null, null, null);

            var customerName = invoice.Customer?.FullName ?? invoice.Customer?.CompanyName;

            var lines = invoice.Lines
                .Select(l => new InvoiceEmailLine(l.Description, l.Quantity, l.UnitPrice, l.LineTotal))
                .ToList();

            var message = new InvoiceSentEmail(
                email,
                customerName,
                invoice.InvoiceNumber,
                invoice.InvoiceMonth,
                invoice.DueOn,
                invoice.Subtotal,
                invoice.VatAmount,
                invoice.Total,
                lines,
                companyDetails)
            {
                IdempotencyKey = $"invoice-sent:{invoice.Id}:{invoice.SentAt:O}",
            };

            await emailSender.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send the invoice-sent email for invoice {InvoiceNumber}.",
                invoice.InvoiceNumber);
        }
    }

    public static class ErrorCodes
    {
        public const string NotFound = "SendInvoiceCommand.NotFound";
        public const string InvalidStatus = "SendInvoiceCommand.InvalidStatus";
    }
}
