using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Bookings;

internal sealed class BookingRequestedDispatcher(
    ICustomerEngagementClient engagement,
    CustomerEngagementSettings settings,
    ILogger<BookingRequestedDispatcher> logger) : IBookingRequestedDispatcher
{
    public async Task DispatchAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<BookingRequestedPayload>(payloadJson, OutboxJson.Options)
            ?? throw new InvalidOperationException("Booking-requested outbox payload deserialized to null.");

        var properties = BookingRequestedNotifier.BuildProperties(payload);

        await BookingRequestedNotifier.SendAsync(
            engagement,
            settings,
            logger,
            payload.Email,
            payload.FullName,
            properties,
            $"invoice {payload.Response.InvoiceNumber}",
            cancellationToken);
    }
}
