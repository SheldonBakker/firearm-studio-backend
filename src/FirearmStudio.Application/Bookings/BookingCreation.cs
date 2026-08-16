using System.Globalization;
using ErrorOr;
using FirearmStudio.Application.Common;
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

    internal sealed record OperatingHoursEntry(DayOfWeek Day, TimeOnly OpenTime, TimeOnly CloseTime);

    internal sealed record RangeData(
        string Name,
        int LaneCount,
        int SlotIntervalMinutes,
        IReadOnlyList<OperatingHoursEntry> OperatingHours);

    internal sealed record PackageData(
        string Name,
        decimal Price,
        int DurationMinutes,
        int MaxShooters,
        IReadOnlyList<BookingInvoiceFactory.IncludedItem> Items);

    internal sealed record OccupancyWindow(
        Guid ShootingRangeId,
        DateOnly BookingDate,
        TimeOnly StartTime,
        TimeOnly EndTime);

    internal static ErrorOr<BookingInvoiceFactory.BookingLine> CreateBooking(
        SlotRequest request,
        RangeData? range,
        PackageData? package,
        IReadOnlyCollection<OccupancyWindow> occupancyWindows,
        IReadOnlyCollection<Booking> pendingBookings,
        long bookingNumber,
        DateTime nowSast)
    {
        var earliestStart = BookingCutoff.EarliestStart(request.BookingDate, nowSast);
        if (earliestStart is null)
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

        if (request.StartTime < earliestStart.Value)
        {
            return Error.Validation(
                ErrorCodes.StartTimeTooSoon,
                $"Bookings must be made at least {BookingCutoff.LeadTimeMinutes} minutes in advance.");
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
            CalendarToken = CalendarTokenGenerator.Generate(),
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
        public const string StartTimeTooSoon = "CreateBooking.StartTimeTooSoon";
    }
}
