using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Bookings;

internal sealed record BookingRequestedPayload(
    string Email,
    string? FullName,
    PublicBookingResponse Response,
    IReadOnlyList<BookingRequestedBookingDetail> BookingDetails,
    CompanyNotificationData Company);

/// <summary>
/// Per-booking notification-only data that does not belong in the public HTTP response
/// (<see cref="PublicBookingResponse"/>): calendar links and deposit terms, keyed by booking ID so
/// <see cref="BookingRequestedNotifier"/> can join it back onto <c>Response.Bookings</c>.
/// </summary>
internal sealed record BookingRequestedBookingDetail(
    Guid BookingId,
    string? IcsUrl,
    string? GoogleCalendarUrl,
    decimal? DepositAmount,
    DateTime? DepositDueAt);

internal sealed record CompanyNotificationData(
    Guid Id,
    string? Name,
    string? RegistrationNumber,
    string? VatNumber,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Province,
    string? PostalCode,
    string? BankName,
    string? BankAccountHolder,
    string? BankAccountNumber,
    string? BankBranchCode,
    string? BankAccountType,
    string? BankSwiftCode)
{
    public static CompanyNotificationData From(Company company) => new(
        company.Id,
        company.Name,
        company.RegistrationNumber,
        company.VatNumber,
        company.Email,
        company.Phone,
        company.AddressLine1,
        company.AddressLine2,
        company.City,
        company.Province,
        company.PostalCode,
        company.BankName,
        company.BankAccountHolder,
        company.BankAccountNumber,
        company.BankBranchCode,
        company.BankAccountType,
        company.BankSwiftCode);
}
