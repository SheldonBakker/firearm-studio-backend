using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.CreatePublicBooking;

public sealed class CreatePublicBookingCommandHandler(
    IApplicationDbContext db,
    ITenantContext tenant,
    IBookingRequestedOutbox outbox)
    : ICommandHandler<CreatePublicBookingCommand, ErrorOr<PublicBookingResponse>>
{
    public async Task<ErrorOr<PublicBookingResponse>> Handle(
        CreatePublicBookingCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var company = await db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == command.CompanyId && c.IsActive, cancellationToken);

        if (company is null)
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
            var fullName = request.FullName.Trim();
            var normalizedEmail = email.ToLower();

            var customer = await db.Customers
                .FirstOrDefaultAsync(c => c.Email != null && c.Email.ToLower() == normalizedEmail, ct);

            if (customer is null)
            {
                customer = new Customer
                {
                    Id = Guid.CreateVersion7(),
                    CustomerType = CustomerType.Individual,
                    FullName = fullName,
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

                customer.FullName ??= fullName;
                customer.Phone ??= request.Phone;
            }

            var invoiceLines = new List<BookingInvoiceFactory.BookingLine>(request.Sessions.Count);
            var pendingBookings = new List<Booking>(request.Sessions.Count);

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
                    pendingBookings,
                    ct);

                if (result.IsError)
                {
                    outcome = result.Errors;
                    return;
                }

                invoiceLines.Add(result.Value);
                pendingBookings.Add(result.Value.Booking);
            }

            var invoice = BookingInvoiceFactory.CreateCombined(invoiceLines, company.VatNumber, company.DueDays);
            db.Invoices.Add(invoice);

            foreach (var line in invoiceLines)
            {
                line.Booking.InvoiceId = invoice.Id;
            }

            var response = new PublicBookingResponse(
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
                    .ToList(),
                new PublicInvoiceBankingResponse(
                    company.BankName,
                    company.BankAccountHolder,
                    company.BankAccountNumber,
                    company.BankBranchCode,
                    company.BankAccountType,
                    company.BankSwiftCode));

            outbox.Add(company, email, fullName, response);

            await db.SaveChangesAsync(ct);

            outcome = response;
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

        return outcome;
    }

    public static class ErrorCodes
    {
        public const string CompanyNotFound = "CreatePublicBookingCommand.CompanyNotFound";
        public const string CustomerInactive = "CreatePublicBookingCommand.CustomerInactive";
    }
}
