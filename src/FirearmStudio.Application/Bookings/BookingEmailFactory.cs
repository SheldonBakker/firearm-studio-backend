using FirearmStudio.Application.Abstractions.Email;

namespace FirearmStudio.Application.Bookings;

internal static class BookingEmailFactory
{
    internal static BookingRequestedEmail BuildRequestedEmail(BookingRequestedPayload payload)
    {
        var response = payload.Response;
        var detailsByBookingId = payload.BookingDetails.ToDictionary(d => d.BookingId);

        var sessions = response.Bookings
            .Select(booking =>
            {
                detailsByBookingId.TryGetValue(booking.Id, out var detail);
                return new BookingEmailSession(
                    booking.BookingNumber,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.EndTime,
                    booking.RangeName,
                    booking.PackageName,
                    booking.PackagePrice,
                    detail?.IcsUrl,
                    detail?.GoogleCalendarUrl,
                    detail?.DepositAmount,
                    detail?.DepositDueAt);
            })
            .ToList();

        return new BookingRequestedEmail(
            payload.Email,
            payload.FullName,
            response.InvoiceNumber,
            response.Subtotal,
            response.VatAmount,
            response.Total,
            sessions,
            MapCompany(payload.Company));
    }

    internal static BookingLifecycleEmail BuildLifecycleEmail(
        BookingLifecyclePayload payload,
        BookingLifecycleKind kind)
    {
        var session = new BookingEmailSession(
            payload.BookingNumber,
            payload.BookingDate,
            payload.StartTime,
            payload.EndTime,
            payload.RangeName,
            payload.PackageName,
            payload.PackagePrice,
            payload.IcsUrl,
            payload.GoogleCalendarUrl,
            payload.DepositAmount,
            payload.DepositDueAt);

        return new BookingLifecycleEmail(
            kind,
            payload.Email,
            payload.FullName,
            session,
            payload.ShooterCount,
            payload.InvoiceNumber,
            MapCompany(payload.Company));
    }

    private static CompanyEmailDetails MapCompany(CompanyNotificationData? company) =>
        company is null
            ? new CompanyEmailDetails(null, null, null, null, null, null, null, null)
            : new CompanyEmailDetails(
                company.Name,
                company.Email,
                company.Phone,
                company.BankName,
                company.BankAccountHolder,
                company.BankAccountNumber,
                company.BankBranchCode,
                company.BankAccountType);
}
