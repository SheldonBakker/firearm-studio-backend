using System.Linq.Expressions;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Bookings;

public sealed record BookingResponse(
    Guid Id,
    string BookingNumber,
    Guid ShootingRangeId,
    string? RangeName,
    Guid PackageId,
    string PackageName,
    decimal PackagePrice,
    Guid CustomerId,
    string? CustomerName,
    Guid? InvoiceId,
    DateOnly BookingDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    BookingStatus Status,
    BookingSource Source,
    int ShooterCount,
    string? Notes,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    DateTime? ReminderSentAt,
    DateTime? CheckedInAt,
    DateTime CreatedAt)
{
    public static Expression<Func<Booking, BookingResponse>> QueryProjection => b => new BookingResponse(
        b.Id, b.BookingNumber,
        b.ShootingRangeId, b.ShootingRange!.Name,
        b.PackageId, b.PackageName, b.PackagePrice,
        b.CustomerId, b.Customer!.CustomerType == CustomerType.Company ? b.Customer.CompanyName : b.Customer.FullName,
        b.InvoiceId,
        b.BookingDate, b.StartTime, b.EndTime,
        b.Status, b.Source, b.ShooterCount, b.Notes,
        b.ConfirmedAt, b.CancelledAt, b.ReminderSentAt, b.CheckedInAt, b.CreatedAt);
}

public sealed record BookingListItemDto(
    Guid Id,
    string BookingNumber,
    Guid ShootingRangeId,
    string? RangeName,
    string PackageName,
    decimal PackagePrice,
    Guid CustomerId,
    string? CustomerName,
    DateOnly BookingDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    BookingStatus Status,
    BookingSource Source)
{
    public static Expression<Func<Booking, BookingListItemDto>> QueryProjection => b => new BookingListItemDto(
        b.Id, b.BookingNumber,
        b.ShootingRangeId, b.ShootingRange!.Name,
        b.PackageName, b.PackagePrice,
        b.CustomerId, b.Customer!.CustomerType == CustomerType.Company ? b.Customer.CompanyName : b.Customer.FullName,
        b.BookingDate, b.StartTime, b.EndTime, b.Status, b.Source);
}

public sealed record BookingCalendarItemDto(
    Guid Id,
    string BookingNumber,
    Guid ShootingRangeId,
    DateOnly BookingDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    BookingStatus Status,
    string? CustomerName)
{
    public static Expression<Func<Booking, BookingCalendarItemDto>> QueryProjection => b => new BookingCalendarItemDto(
        b.Id, b.BookingNumber, b.ShootingRangeId, b.BookingDate, b.StartTime, b.EndTime, b.Status,
        b.Customer!.CustomerType == CustomerType.Company ? b.Customer.CompanyName : b.Customer.FullName);
}

public sealed record AvailabilitySlotDto(TimeOnly StartTime, TimeOnly EndTime, int RemainingLanes);

public sealed record DayAvailabilityResponse(DateOnly Date, IReadOnlyList<AvailabilitySlotDto> Slots);

public sealed record MonthAvailabilityDayDto(DateOnly Date, bool HasAvailability);

public sealed record MonthAvailabilityResponse(IReadOnlyList<MonthAvailabilityDayDto> Days);

public sealed record CreateBookingRequest(
    Guid ShootingRangeId,
    Guid PackageId,
    Guid CustomerId,
    DateOnly BookingDate,
    TimeOnly StartTime,
    int ShooterCount,
    string? Notes,
    bool ConfirmImmediately);

public sealed record PublicBookingConfirmationResponse(
    Guid Id,
    string BookingNumber,
    BookingStatus Status,
    DateOnly BookingDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string RangeName,
    string PackageName,
    decimal PackagePrice);

public sealed record PublicBookingSessionRequest(
    Guid ShootingRangeId,
    Guid PackageId,
    DateOnly BookingDate,
    TimeOnly StartTime,
    int ShooterCount,
    string? Notes);

public sealed record CreatePublicBookingRequest(
    IReadOnlyList<PublicBookingSessionRequest> Sessions,
    string FullName,
    string Email,
    string? Phone,
    string? Notes);

public sealed record PublicBookingResponse(
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total,
    decimal? DepositAmount,
    DateTime? DepositDueAt,
    IReadOnlyList<PublicBookingConfirmationResponse> Bookings,
    PublicInvoiceBankingResponse Banking);

public sealed record PublicInvoiceBankingResponse(
    string? BankName,
    string? BankAccountHolder,
    string? BankAccountNumber,
    string? BankBranchCode,
    string? BankAccountType,
    string? BankSwiftCode);

public sealed record AttendeeRequest(
    string FullName,
    string IdNumber,
    string? LicenceNumber,
    string? FirearmMakeModel,
    string? FirearmSerialNumber,
    string? Calibre,
    FirearmOrigin FirearmOrigin,
    bool SignedIndemnity,
    string? Notes);

public sealed record AttendeeResponse(
    Guid Id,
    Guid BookingId,
    string FullName,
    string IdNumber,
    string? LicenceNumber,
    string? FirearmMakeModel,
    string? FirearmSerialNumber,
    string? Calibre,
    FirearmOrigin FirearmOrigin,
    bool SignedIndemnity,
    string? Notes,
    DateTime CreatedAt)
{
    public static Expression<Func<BookingAttendee, AttendeeResponse>> QueryProjection => a => new AttendeeResponse(
        a.Id, a.BookingId, a.FullName, a.IdNumber,
        a.LicenceNumber, a.FirearmMakeModel, a.FirearmSerialNumber, a.Calibre,
        a.FirearmOrigin, a.SignedIndemnity, a.Notes, a.CreatedAt);
}

public sealed record UpdateAttendeeRequest(
    Optional<string> FullName,
    Optional<string> IdNumber,
    Optional<string?> LicenceNumber,
    Optional<string?> FirearmMakeModel,
    Optional<string?> FirearmSerialNumber,
    Optional<string?> Calibre,
    Optional<FirearmOrigin> FirearmOrigin,
    Optional<bool> SignedIndemnity,
    Optional<string?> Notes);

public sealed record CheckInBookingRequest(IReadOnlyList<AttendeeRequest> Attendees);

public sealed record CreateAttendeeResponse(Guid Id);
