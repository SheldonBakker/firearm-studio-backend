using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Bookings;

internal sealed class BookingRequestedOutbox(IApplicationDbContext db) : IBookingRequestedOutbox
{
    public void Add(Company company, string email, string? fullName, PublicBookingResponse response)
    {
        var payload = new BookingRequestedPayload(
            email,
            fullName,
            response,
            CompanyNotificationData.From(company));

        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = OutboxMessageTypes.BookingRequested,
            Payload = JsonSerializer.Serialize(payload, OutboxJson.Options),
            CompanyId = company.Id,
        });
    }
}
