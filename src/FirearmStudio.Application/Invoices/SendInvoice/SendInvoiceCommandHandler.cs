using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Invoices.SendInvoice;

public sealed class SendInvoiceCommandHandler(
    IApplicationDbContext db,
    IKlaviyoClient klaviyo,
    KlaviyoSettings settings,
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

        await SendKlaviyoEventAsync(invoice, cancellationToken);

        return Result.Updated;
    }

    private async Task SendKlaviyoEventAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var email = invoice.Customer?.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning(
                "Skipped Klaviyo invoice-sent event for invoice {InvoiceNumber}: customer has no email.",
                invoice.InvoiceNumber);
            return;
        }

        try
        {
            var company = await db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == invoice.CompanyId, cancellationToken);

            var name = invoice.Customer?.FullName ?? invoice.Customer?.CompanyName;
            var properties = BuildEventProperties(invoice, company);

            await klaviyo.TrackEventAsync(
                settings.InvoiceSentMetricName,
                email,
                name,
                properties,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send invoice-sent event to Klaviyo for invoice {InvoiceNumber}.",
                invoice.InvoiceNumber);
        }
    }

    private static Dictionary<string, object?> BuildEventProperties(Invoice invoice, Company? company)
    {
        var properties = new Dictionary<string, object?>
        {
            ["invoice_id"] = invoice.Id,
            ["invoice_number"] = invoice.InvoiceNumber,
            ["invoice_month"] = invoice.InvoiceMonth.ToString("yyyy-MM-dd"),
            ["status"] = invoice.Status.ToString(),
            ["subtotal"] = invoice.Subtotal,
            ["vat_amount"] = invoice.VatAmount,
            ["total"] = invoice.Total,
            ["sent_at"] = invoice.SentAt,
            ["due_on"] = invoice.DueOn?.ToString("yyyy-MM-dd"),
            ["lines"] = invoice.Lines
                .Select(line => new Dictionary<string, object?>
                {
                    ["description"] = line.Description,
                    ["quantity"] = line.Quantity,
                    ["unit_price"] = line.UnitPrice,
                    ["line_total"] = line.LineTotal,
                })
                .ToList(),
        };

        if (invoice.Customer is { } customer)
        {
            properties["customer"] = new Dictionary<string, object?>
            {
                ["id"] = customer.Id,
                ["type"] = customer.CustomerType.ToString(),
                ["full_name"] = customer.FullName,
                ["company_name"] = customer.CompanyName,
                ["registration_number"] = customer.RegistrationNumber,
                ["vat_number"] = customer.VatNumber,
                ["email"] = customer.Email,
                ["phone"] = customer.Phone,
                ["address_line1"] = customer.AddressLine1,
                ["address_line2"] = customer.AddressLine2,
                ["city"] = customer.City,
                ["province"] = customer.Province,
                ["postal_code"] = customer.PostalCode,
            };
        }

        if (company is not null)
        {
            properties["company"] = new Dictionary<string, object?>
            {
                ["id"] = company.Id,
                ["name"] = company.Name,
                ["registration_number"] = company.RegistrationNumber,
                ["vat_number"] = company.VatNumber,
                ["email"] = company.Email,
                ["phone"] = company.Phone,
                ["address_line1"] = company.AddressLine1,
                ["address_line2"] = company.AddressLine2,
                ["city"] = company.City,
                ["province"] = company.Province,
                ["postal_code"] = company.PostalCode,
                ["bank_name"] = company.BankName,
                ["bank_account_holder"] = company.BankAccountHolder,
                ["bank_account_number"] = company.BankAccountNumber,
                ["bank_branch_code"] = company.BankBranchCode,
                ["bank_account_type"] = company.BankAccountType,
                ["bank_swift_code"] = company.BankSwiftCode,
            };
        }

        return properties;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "SendInvoiceCommand.NotFound";
        public const string InvalidStatus = "SendInvoiceCommand.InvalidStatus";
    }
}
