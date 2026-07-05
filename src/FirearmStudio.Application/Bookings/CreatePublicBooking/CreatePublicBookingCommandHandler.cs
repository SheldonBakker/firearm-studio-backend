using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.CreatePublicBooking;

public sealed class CreatePublicBookingCommandHandler(IApplicationDbContext db, ITenantContext tenant)
    : ICommandHandler<CreatePublicBookingCommand, ErrorOr<PublicBookingConfirmationResponse>>
{
    private const int MaxPendingBookingsPerDay = 3;

    public async Task<ErrorOr<PublicBookingConfirmationResponse>> Handle(
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

        ErrorOr<PublicBookingConfirmationResponse> outcome = Error.Conflict(
            BookingCreation.ErrorCodes.SlotContention,
            "The slot could not be reserved due to concurrent bookings. Please retry.");

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

                var pendingOnDate = await db.Bookings
                    .CountAsync(b => b.CustomerId == customer.Id
                                     && b.BookingDate == request.BookingDate
                                     && b.Status == BookingStatus.Pending, ct);

                if (pendingOnDate >= MaxPendingBookingsPerDay)
                {
                    outcome = Error.Conflict(
                        ErrorCodes.TooManyPendingBookings,
                        "Too many pending bookings for this customer on the selected date.");
                    return;
                }
            }

            var result = await BookingCreation.AddBookingAsync(
                db,
                new BookingCreation.SlotRequest(
                    request.ShootingRangeId,
                    request.PackageId,
                    customer.Id,
                    request.BookingDate,
                    request.StartTime,
                    request.ShooterCount,
                    request.Notes,
                    BookingSource.Public),
                ct);

            if (result.IsError)
            {
                outcome = result.Errors;
                return;
            }

            var booking = result.Value;

            await db.SaveChangesAsync(ct);

            var rangeName = await db.ShootingRanges
                .AsNoTracking()
                .Where(r => r.Id == booking.ShootingRangeId)
                .Select(r => r.Name)
                .FirstAsync(ct);

            outcome = new PublicBookingConfirmationResponse(
                booking.Id,
                booking.BookingNumber,
                booking.Status,
                booking.BookingDate,
                booking.StartTime,
                booking.EndTime,
                rangeName,
                booking.PackageName,
                booking.PackagePrice);
        }, cancellationToken);

        if (outcome.IsError)
        {
            return outcome.Errors;
        }

        if (!committed)
        {
            return Error.Conflict(
                BookingCreation.ErrorCodes.SlotContention,
                "The slot could not be reserved due to concurrent bookings. Please retry.");
        }

        return outcome;
    }

    public static class ErrorCodes
    {
        public const string CompanyNotFound = "CreatePublicBookingCommand.CompanyNotFound";
        public const string CustomerInactive = "CreatePublicBookingCommand.CustomerInactive";
        public const string TooManyPendingBookings = "CreatePublicBookingCommand.TooManyPendingBookings";
    }
}
