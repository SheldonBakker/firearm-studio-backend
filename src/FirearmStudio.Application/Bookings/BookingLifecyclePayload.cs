namespace FirearmStudio.Application.Bookings;

internal sealed record BookingLifecyclePayload(
    string Email,
    string? FullName,
    Guid BookingId,
    string BookingNumber,
    DateOnly BookingDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? RangeName,
    string PackageName,
    decimal PackagePrice,
    int ShooterCount,
    string? IcsUrl,
    string? GoogleCalendarUrl,
    decimal? DepositAmount,
    DateTime? DepositDueAt,
    string? InvoiceNumber,
    CompanyNotificationData Company);
