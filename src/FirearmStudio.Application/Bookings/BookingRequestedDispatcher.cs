using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Bookings;

internal sealed class BookingRequestedDispatcher(
    ITransactionalEmailSender emailSender,
    ILogger<BookingRequestedDispatcher> logger) : IBookingRequestedDispatcher
{
    public async Task DispatchAsync(Guid outboxMessageId, string payloadJson, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<BookingRequestedPayload>(payloadJson, OutboxJson.Options)
            ?? throw new InvalidOperationException("Booking-requested outbox payload deserialized to null.");

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            logger.LogWarning(
                "Skipped booking-requested email for invoice {InvoiceNumber}: customer has no email.",
                payload.Response.InvoiceNumber);
            return;
        }

        var message = BookingEmailFactory.BuildRequestedEmail(payload) with
        {
            IdempotencyKey = $"outbox:{outboxMessageId}",
        };

        await emailSender.SendAsync(message, cancellationToken);
    }
}
