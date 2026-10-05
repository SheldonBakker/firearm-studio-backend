using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Bookings;

internal sealed class BookingLifecycleDispatcher(
    ITransactionalEmailSender emailSender,
    ILogger<BookingLifecycleDispatcher> logger) : IBookingLifecycleDispatcher
{
    public async Task DispatchAsync(Guid outboxMessageId, string messageType, string payloadJson, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<BookingLifecyclePayload>(payloadJson, OutboxJson.Options)
            ?? throw new InvalidOperationException($"{messageType} outbox payload deserialized to null.");

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            logger.LogWarning(
                "Skipped {MessageType} email for booking {BookingNumber}: customer has no email.",
                messageType, payload.BookingNumber);
            return;
        }

        var kind = KindFor(messageType);

        var message = BookingEmailFactory.BuildLifecycleEmail(payload, kind) with
        {
            IdempotencyKey = $"outbox:{outboxMessageId}",
        };

        await emailSender.SendAsync(message, cancellationToken);
    }

    private static BookingLifecycleKind KindFor(string messageType) => messageType switch
    {
        OutboxMessageTypes.BookingConfirmed => BookingLifecycleKind.Confirmed,
        OutboxMessageTypes.BookingReminder => BookingLifecycleKind.Reminder,
        OutboxMessageTypes.BookingCancelled => BookingLifecycleKind.Cancelled,
        _ => throw new InvalidOperationException($"Unknown booking lifecycle message type '{messageType}'."),
    };
}
