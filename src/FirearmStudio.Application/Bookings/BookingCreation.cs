using System.Globalization;
using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings;

internal static class BookingCreation
{
    internal sealed record SlotRequest(
        Guid ShootingRangeId,
        Guid PackageId,
        Guid CustomerId,
        DateOnly BookingDate,
        TimeOnly StartTime,
        int ShooterCount,
        string? Notes,
        BookingSource Source);

    internal static async Task<ErrorOr<Booking>> AddBookingAsync(
        IApplicationDbContext db, SlotRequest request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (request.BookingDate < today)
        {
            return Error.Validation(ErrorCodes.DateInPast, "Booking date may not be in the past.");
        }

        var range = await db.ShootingRanges
            .AsNoTracking()
            .Where(r => r.Id == request.ShootingRangeId && r.IsActive)
            .Select(r => new
            {
                r.LaneCount,
                r.SlotIntervalMinutes,
                Hours = r.OperatingHours
                    .Where(h => h.Day == request.BookingDate.DayOfWeek)
                    .Select(h => new { h.OpenTime, h.CloseTime })
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (range is null)
        {
            return Error.NotFound(ErrorCodes.RangeNotFound, "Shooting range not found.");
        }

        var package = await db.Packages
            .AsNoTracking()
            .Where(p => p.Id == request.PackageId && p.IsActive)
            .Select(p => new { p.Name, p.Price, p.DurationMinutes, p.MaxShooters })
            .FirstOrDefaultAsync(cancellationToken);

        if (package is null)
        {
            return Error.NotFound(ErrorCodes.PackageNotFound, "Package not found.");
        }

        if (request.ShooterCount > package.MaxShooters)
        {
            return Error.Validation(
                ErrorCodes.TooManyShooters,
                $"The package allows at most {package.MaxShooters} shooter(s).");
        }

        if (range.Hours is null)
        {
            return Error.Validation(ErrorCodes.OutsideOperatingHours, "The range is closed on that day.");
        }

        if (!AvailabilityCalculator.IsOnSlotGrid(range.Hours.OpenTime, request.StartTime, range.SlotIntervalMinutes))
        {
            return Error.Validation(
                ErrorCodes.InvalidStartTime,
                $"Start time must align to the range's {range.SlotIntervalMinutes}-minute slot grid.");
        }

        var endTime = request.StartTime.AddMinutes(package.DurationMinutes);
        if (request.StartTime < range.Hours.OpenTime
            || endTime > range.Hours.CloseTime
            || endTime <= request.StartTime)
        {
            return Error.Validation(
                ErrorCodes.OutsideOperatingHours,
                "The requested time window falls outside the range's operating hours.");
        }

        var overlapping = await db.Bookings
            .Where(b => b.ShootingRangeId == request.ShootingRangeId
                        && b.BookingDate == request.BookingDate
                        && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                        && b.StartTime < endTime
                        && b.EndTime > request.StartTime)
            .CountAsync(cancellationToken);

        if (overlapping >= range.LaneCount)
        {
            return Error.Conflict(ErrorCodes.SlotUnavailable, "No lane is available for the requested time.");
        }

        var bookingsOnDate = await db.Bookings
            .Where(b => b.BookingDate == request.BookingDate)
            .CountAsync(cancellationToken);

        var dateLabel = request.BookingDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        var booking = new Booking
        {
            ShootingRangeId = request.ShootingRangeId,
            PackageId = request.PackageId,
            CustomerId = request.CustomerId,
            BookingNumber = $"BKG-{dateLabel}-{bookingsOnDate + 1:D4}",
            BookingDate = request.BookingDate,
            StartTime = request.StartTime,
            EndTime = endTime,
            Source = request.Source,
            PackageName = package.Name,
            PackagePrice = package.Price,
            ShooterCount = request.ShooterCount,
            Notes = request.Notes,
        };

        await db.Bookings.AddAsync(booking, cancellationToken);

        return booking;
    }

    public static class ErrorCodes
    {
        public const string DateInPast = "CreateBooking.DateInPast";
        public const string RangeNotFound = "CreateBooking.RangeNotFound";
        public const string PackageNotFound = "CreateBooking.PackageNotFound";
        public const string TooManyShooters = "CreateBooking.TooManyShooters";
        public const string OutsideOperatingHours = "CreateBooking.OutsideOperatingHours";
        public const string InvalidStartTime = "CreateBooking.InvalidStartTime";
        public const string SlotUnavailable = "CreateBooking.SlotUnavailable";
        public const string SlotContention = "CreateBooking.SlotContention";
    }
}
