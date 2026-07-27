using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Bookings;

internal sealed class BookingRequestedOutbox(IApplicationDbContext db, NotificationSettings settings)
    : IBookingRequestedOutbox
{
    public void Add(
        Company company,
        string email,
        string? fullName,
        PublicBookingResponse response,
        IReadOnlyList<Booking> bookings)
    {
        var bookingsById = bookings.ToDictionary(b => b.Id);
        var companyIcsData = new BookingIcsBuilder.CompanyIcsData(
            company.Name, company.AddressLine1, company.AddressLine2, company.City, company.Province,
            company.PostalCode);

        var bookingDetails = response.Bookings
            .Select(line =>
            {
                if (!bookingsById.TryGetValue(line.Id, out var booking))
                {
                    return new BookingRequestedBookingDetail(line.Id, null, null, response.DepositAmount, response.DepositDueAt);
                }

                var links = BookingCalendarLinkBuilder.Build(
                    settings.PublicBaseUrl,
                    booking.CalendarToken,
                    new BookingIcsBuilder.BookingIcsData(
                        line.Id,
                        line.BookingNumber,
                        line.PackageName,
                        line.RangeName,
                        line.BookingDate,
                        line.StartTime,
                        line.EndTime,
                        booking.ShooterCount),
                    companyIcsData);

                // The deposit applies to the combined invoice, so every booking on it shares the
                // same deposit terms.
                return new BookingRequestedBookingDetail(
                    line.Id, links.IcsUrl, links.GoogleCalendarUrl, response.DepositAmount, response.DepositDueAt);
            })
            .ToList();

        var payload = new BookingRequestedPayload(
            email,
            fullName,
            response,
            bookingDetails,
            CompanyNotificationData.From(company));

        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = OutboxMessageTypes.BookingRequested,
            Payload = JsonSerializer.Serialize(payload, OutboxJson.Options),
            CompanyId = company.Id,
        });
    }
}
