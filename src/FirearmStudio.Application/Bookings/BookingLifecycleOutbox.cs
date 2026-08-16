using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Bookings;

internal sealed class BookingLifecycleOutbox(IApplicationDbContext db) : IBookingLifecycleOutbox
{
    public void Add(
        string messageType,
        Company company,
        Booking booking,
        string? rangeName,
        string email,
        string? fullName,
        string? icsUrl,
        string? googleCalendarUrl,
        decimal? depositAmount,
        DateTime? depositDueAt,
        string? invoiceNumber)
    {
        var payload = new BookingLifecyclePayload(
            email,
            fullName,
            booking.Id,
            booking.BookingNumber,
            booking.BookingDate,
            booking.StartTime,
            booking.EndTime,
            rangeName,
            booking.PackageName,
            booking.PackagePrice,
            booking.ShooterCount,
            icsUrl,
            googleCalendarUrl,
            depositAmount,
            depositDueAt,
            invoiceNumber,
            CompanyNotificationData.From(company));

        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = messageType,
            Payload = JsonSerializer.Serialize(payload, OutboxJson.Options),
        });
    }
}
