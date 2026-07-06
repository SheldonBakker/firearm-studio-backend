using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Bookings.CreatePublicBooking;

public sealed class CreatePublicBookingCommandHandler(
    IApplicationDbContext db,
    ITenantContext tenant,
    IKlaviyoClient klaviyo,
    KlaviyoSettings settings,
    ILogger<CreatePublicBookingCommandHandler> logger)
    : ICommandHandler<CreatePublicBookingCommand, ErrorOr<PublicBookingResponse>>
{
    public async Task<ErrorOr<PublicBookingResponse>> Handle(
        CreatePublicBookingCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var companyExists = await db.Companies
            .AsNoTracking()
            .AnyAsync(c => c.Id == command.CompanyId && c.IsActive, cancellationToken);

        if (!companyExists)
        {
            return Error.NotFound(ErrorCodes.CompanyNotFound, "Company not found.");
        }

        using var scope = tenant.BeginCompanyScope(command.CompanyId);

        ErrorOr<PublicBookingResponse> outcome = Error.Conflict(
            BookingCreation.ErrorCodes.SlotContention,
            "The bookings could not be reserved due to concurrent bookings. Please retry.");

        var committed = await db.TryExecuteInSerializableTransactionAsync(async ct =>
        {
            var email = request.Email.Trim();
            var normalizedEmail = email.ToLower();

            var customer = await db.Customers
                .FirstOrDefaultAsync(c => c.Email != null && c.Email.ToLower() == normalizedEmail, ct);

            if (customer is null)
            {
                customer = new Customer
                {
                    Id = Guid.CreateVersion7(),
                    CustomerType = CustomerType.Individual,
                    FullName = request.FullName.Trim(),
                    Email = email,
                    Phone = request.Phone,
                };
                await db.Customers.AddAsync(customer, ct);
            }
            else
            {
                if (!customer.IsActive)
                {
                    outcome = Error.Conflict(ErrorCodes.CustomerInactive, "This customer account is inactive.");
                    return;
                }

                customer.FullName ??= request.FullName.Trim();
                customer.Phone ??= request.Phone;
            }

            var invoiceLines = new List<BookingInvoiceFactory.BookingLine>(request.Sessions.Count);

            foreach (var session in request.Sessions)
            {
                var result = await BookingCreation.AddBookingAsync(
                    db,
                    new BookingCreation.SlotRequest(
                        session.ShootingRangeId,
                        session.PackageId,
                        customer.Id,
                        session.BookingDate,
                        session.StartTime,
                        session.ShooterCount,
                        session.Notes,
                        BookingSource.Public),
                    ct);

                if (result.IsError)
                {
                    outcome = result.Errors;
                    return;
                }

                var booking = result.Value;

                // Persist each booking before creating the next so its lane occupancy and
                // per-date booking-number sequence are visible to the following iterations.
                await db.SaveChangesAsync(ct);

                var rangeName = await db.ShootingRanges
                    .AsNoTracking()
                    .Where(r => r.Id == booking.ShootingRangeId)
                    .Select(r => r.Name)
                    .FirstAsync(ct);

                var packageItems = await db.PackageItems
                    .AsNoTracking()
                    .Where(i => i.PackageId == booking.PackageId)
                    .OrderBy(i => i.SortOrder)
                    .ThenBy(i => i.Id)
                    .Select(i => new BookingInvoiceFactory.IncludedItem(i.Description, i.Quantity))
                    .ToListAsync(ct);

                invoiceLines.Add(new BookingInvoiceFactory.BookingLine(booking, rangeName, packageItems));
            }

            var company = await db.Companies
                .AsNoTracking()
                .Where(c => c.Id == tenant.CompanyId)
                .Select(c => new { c.VatNumber, c.DueDays })
                .FirstAsync(ct);

            var invoice = BookingInvoiceFactory.CreateCombined(invoiceLines, company.VatNumber, company.DueDays);
            db.Invoices.Add(invoice);

            foreach (var line in invoiceLines)
            {
                line.Booking.InvoiceId = invoice.Id;
            }

            await db.SaveChangesAsync(ct);

            outcome = new PublicBookingResponse(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Subtotal,
                invoice.VatAmount,
                invoice.Total,
                invoiceLines
                    .Select(line => new PublicBookingConfirmationResponse(
                        line.Booking.Id,
                        line.Booking.BookingNumber,
                        line.Booking.Status,
                        line.Booking.BookingDate,
                        line.Booking.StartTime,
                        line.Booking.EndTime,
                        line.RangeName,
                        line.Booking.PackageName,
                        line.Booking.PackagePrice))
                    .ToList());
        }, cancellationToken);

        if (outcome.IsError)
        {
            return outcome.Errors;
        }

        if (!committed)
        {
            return Error.Conflict(
                BookingCreation.ErrorCodes.SlotContention,
                "The bookings could not be reserved due to concurrent bookings. Please retry.");
        }

        await NotifyBookingRequestedAsync(command.CompanyId, request, outcome.Value, cancellationToken);

        return outcome;
    }

    private async Task NotifyBookingRequestedAsync(
        Guid companyId,
        CreatePublicBookingRequest request,
        PublicBookingResponse response,
        CancellationToken cancellationToken)
    {
        var company = await db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

        var properties = new Dictionary<string, object?>
        {
            ["invoice_id"] = response.InvoiceId,
            ["invoice_number"] = response.InvoiceNumber,
            ["session_count"] = response.Bookings.Count,
            ["subtotal"] = response.Subtotal,
            ["vat_amount"] = response.VatAmount,
            ["total"] = response.Total,
            ["bookings"] = response.Bookings
                .Select(booking => new Dictionary<string, object?>
                {
                    ["booking_id"] = booking.Id,
                    ["booking_number"] = booking.BookingNumber,
                    ["status"] = booking.Status.ToString(),
                    ["booking_date"] = booking.BookingDate.ToString("yyyy-MM-dd"),
                    ["start_time"] = booking.StartTime.ToString("HH\\:mm"),
                    ["end_time"] = booking.EndTime.ToString("HH\\:mm"),
                    ["range_name"] = booking.RangeName,
                    ["package_name"] = booking.PackageName,
                    ["package_price"] = booking.PackagePrice,
                })
                .ToList(),
            ["company"] = BookingRequestedNotifier.BuildCompanyProperties(company),
        };

        await BookingRequestedNotifier.NotifyAsync(
            klaviyo,
            settings,
            logger,
            request.Email.Trim(),
            request.FullName.Trim(),
            properties,
            $"invoice {response.InvoiceNumber}",
            cancellationToken);
    }

    public static class ErrorCodes
    {
        public const string CompanyNotFound = "CreatePublicBookingCommand.CompanyNotFound";
        public const string CustomerInactive = "CreatePublicBookingCommand.CustomerInactive";
    }
}
