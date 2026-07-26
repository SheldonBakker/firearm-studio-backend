using System.Globalization;
using ErrorOr;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

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

    /// <summary>Operating hours for a single day of week, pre-loaded with the range.</summary>
    internal sealed record OperatingHoursEntry(DayOfWeek Day, TimeOnly OpenTime, TimeOnly CloseTime);

    /// <summary>
    /// Preloaded shooting range data used by <see cref="CreateBooking"/>. All operating-hours
    /// rows for the range are included so that callers can batch-load ranges for multiple sessions.
    /// </summary>
    internal sealed record RangeData(
        string Name,
        int LaneCount,
        int SlotIntervalMinutes,
        IReadOnlyList<OperatingHoursEntry> OperatingHours);

    /// <summary>Preloaded package data used by <see cref="CreateBooking"/>.</summary>
    internal sealed record PackageData(
        string Name,
        decimal Price,
        int DurationMinutes,
        int MaxShooters,
        IReadOnlyList<BookingInvoiceFactory.IncludedItem> Items);

    /// <summary>
    /// A booked window from the database, used for in-memory lane-occupancy counting.
    /// Includes <see cref="ShootingRangeId"/> and <see cref="BookingDate"/> so that a single
    /// pre-loaded collection can cover multiple (range, date) combinations.
    /// </summary>
    internal sealed record OccupancyWindow(
        Guid ShootingRangeId,
        DateOnly BookingDate,
        TimeOnly StartTime,
        TimeOnly EndTime);

    /// <summary>
    /// Pure in-memory validation and booking construction. No database I/O. Callers are
    /// responsible for pre-loading <paramref name="range"/> and <paramref name="package"/>
    /// (pass <c>null</c> if not found) and for adding the resulting booking to the change tracker.
    /// <para>
    /// <paramref name="occupancyWindows"/> must cover at minimum the (rangeId, date) pair in
    /// <paramref name="request"/>. <paramref name="pendingBookings"/> are same-transaction bookings
    /// not yet saved; they count toward lane occupancy so multi-session carts need no intermediate saves.
    /// </para>
    /// </summary>
    internal static ErrorOr<BookingInvoiceFactory.BookingLine> CreateBooking(
        SlotRequest request,
        RangeData? range,
        PackageData? package,
        IReadOnlyCollection<OccupancyWindow> occupancyWindows,
        IReadOnlyCollection<Booking> pendingBookings,
        long bookingNumber)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (request.BookingDate < today)
        {
            return Error.Validation(ErrorCodes.DateInPast, "Booking date may not be in the past.");
        }

        if (range is null)
        {
            return Error.NotFound(ErrorCodes.RangeNotFound, "Shooting range not found.");
        }

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

        var dayHours = range.OperatingHours.FirstOrDefault(h => h.Day == request.BookingDate.DayOfWeek);

        if (dayHours is null)
        {
            return Error.Validation(ErrorCodes.OutsideOperatingHours, "The range is closed on that day.");
        }

        if (!AvailabilityCalculator.IsOnSlotGrid(dayHours.OpenTime, request.StartTime, range.SlotIntervalMinutes))
        {
            return Error.Validation(
                ErrorCodes.InvalidStartTime,
                $"Start time must align to the range's {range.SlotIntervalMinutes}-minute slot grid.");
        }

        var endTime = request.StartTime.AddMinutes(package.DurationMinutes);
        if (request.StartTime < dayHours.OpenTime
            || endTime > dayHours.CloseTime
            || endTime <= request.StartTime)
        {
            return Error.Validation(
                ErrorCodes.OutsideOperatingHours,
                "The requested time window falls outside the range's operating hours.");
        }

        var overlapping = occupancyWindows.Count(w =>
            w.ShootingRangeId == request.ShootingRangeId
            && w.BookingDate == request.BookingDate
            && w.StartTime < endTime
            && w.EndTime > request.StartTime);

        overlapping += pendingBookings.Count(b =>
            b.ShootingRangeId == request.ShootingRangeId
            && b.BookingDate == request.BookingDate
            && b.StartTime < endTime
            && b.EndTime > request.StartTime);

        if (overlapping >= range.LaneCount)
        {
            return Error.Conflict(ErrorCodes.SlotUnavailable, "No lane is available for the requested time.");
        }

        var dateLabel = request.BookingDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            ShootingRangeId = request.ShootingRangeId,
            PackageId = request.PackageId,
            CustomerId = request.CustomerId,
            BookingNumber = $"BKG-{dateLabel}-{bookingNumber:D4}",
            BookingDate = request.BookingDate,
            StartTime = request.StartTime,
            EndTime = endTime,
            Source = request.Source,
            PackageName = package.Name,
            PackagePrice = package.Price,
            ShooterCount = request.ShooterCount,
            Notes = request.Notes,
        };

        return new BookingInvoiceFactory.BookingLine(booking, range.Name, package.Items);
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
