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

            // --- Batch data loading (all inside the serializable transaction so retries re-read) ---

            // 1. Distinct range IDs -> one query loading ranges with all operating hours.
            var rangeIds = request.Sessions.Select(s => s.ShootingRangeId).Distinct().ToList();
            var rawRanges = await db.ShootingRanges
                .AsNoTracking()
                .Where(r => rangeIds.Contains(r.Id) && r.IsActive)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.LaneCount,
                    r.SlotIntervalMinutes,
                    Hours = r.OperatingHours
                        .Select(h => new { h.Day, h.OpenTime, h.CloseTime })
                        .ToList(),
                })
                .ToListAsync(ct);

            var rangesDict = rawRanges.ToDictionary(
                r => r.Id,
                r => new BookingCreation.RangeData(
                    r.Name,
                    r.LaneCount,
                    r.SlotIntervalMinutes,
                    r.Hours
                        .Select(h => new BookingCreation.OperatingHoursEntry(h.Day, h.OpenTime, h.CloseTime))
                        .ToList()));

            // 2. Distinct package IDs -> one query loading packages with items.
            var packageIds = request.Sessions.Select(s => s.PackageId).Distinct().ToList();
            var rawPackages = await db.Packages
                .AsNoTracking()
                .Where(p => packageIds.Contains(p.Id) && p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.DurationMinutes,
                    p.MaxShooters,
                    Items = p.Items
                        .OrderBy(i => i.SortOrder)
                        .ThenBy(i => i.Id)
                        .Select(i => new BookingInvoiceFactory.IncludedItem(i.Description, i.Quantity))
                        .ToList(),
                })
                .ToListAsync(ct);

            var packagesDict = rawPackages.ToDictionary(
                p => p.Id,
                p => new BookingCreation.PackageData(
                    p.Name,
                    p.Price,
                    p.DurationMinutes,
                    p.MaxShooters,
                    p.Items));

            // 3. One grouped query for existing occupancy windows over involved (rangeId, date) pairs.
            var sessionDates = request.Sessions.Select(s => s.BookingDate).Distinct().ToList();
            var occupancyWindows = await db.Bookings
                .AsNoTracking()
                .Where(b => rangeIds.Contains(b.ShootingRangeId)
                            && sessionDates.Contains(b.BookingDate)
                            && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
                .Select(b => new BookingCreation.OccupancyWindow(
                    b.ShootingRangeId, b.BookingDate, b.StartTime, b.EndTime))
                .ToListAsync(ct);

            // 4. One batched booking-number fetch; numbers are pre-assigned to sessions by index.
            var bookingNumbers = await db.NextBookingNumbersAsync(request.Sessions.Count, ct);

            // ---

            var invoiceLines = new List<BookingInvoiceFactory.BookingLine>(request.Sessions.Count);
            var pendingBookings = new List<Booking>(request.Sessions.Count);

            for (var i = 0; i < request.Sessions.Count; i++)
            {
                var session = request.Sessions[i];

                rangesDict.TryGetValue(session.ShootingRangeId, out var rangeData);
                packagesDict.TryGetValue(session.PackageId, out var packageData);

                var result = BookingCreation.CreateBooking(
                    new BookingCreation.SlotRequest(
                        session.ShootingRangeId,
                        session.PackageId,
                        customer.Id,
                        session.BookingDate,
                        session.StartTime,
                        session.ShooterCount,
                        session.Notes,
                        BookingSource.Public),
                    rangeData,
                    packageData,
                    occupancyWindows,
                    pendingBookings,
                    bookingNumbers[i]);

                if (result.IsError)
                {
                    outcome = result.Errors;
                    return;
                }

                await db.Bookings.AddAsync(result.Value.Booking, ct);
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

            outbox.Add(company, email, fullName, response, pendingBookings);

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
