using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Bookings;

internal sealed class BookingLifecycleDispatcher(
    ICustomerEngagementClient engagement,
    CustomerEngagementSettings settings,
    ILogger<BookingLifecycleDispatcher> logger) : IBookingLifecycleDispatcher
{
    public async Task DispatchAsync(string messageType, string payloadJson, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<BookingLifecyclePayload>(payloadJson, OutboxJson.Options)
            ?? throw new InvalidOperationException($"{messageType} outbox payload deserialized to null.");

        var metricName = MetricNameFor(messageType);

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            logger.LogWarning(
                "Skipped {MessageType} engagement event for booking {BookingNumber}: customer has no email.",
                messageType, payload.BookingNumber);
            return;
        }

        var properties = BuildProperties(payload);

        await engagement.TrackEventAsync(
            metricName,
            payload.Email,
            payload.FullName,
            BookingRequestedNotifier.Flatten(properties),
            cancellationToken);
    }

    private string MetricNameFor(string messageType) => messageType switch
    {
        OutboxMessageTypes.BookingConfirmed => settings.BookingConfirmedMetricName,
        OutboxMessageTypes.BookingReminder => settings.BookingReminderMetricName,
        OutboxMessageTypes.BookingCancelled => settings.BookingCancelledMetricName,
        _ => throw new InvalidOperationException($"Unknown booking lifecycle message type '{messageType}'."),
    };

    private static Dictionary<string, object?> BuildProperties(BookingLifecyclePayload payload) => new()
    {
        ["booking_id"] = payload.BookingId,
        ["booking_number"] = payload.BookingNumber,
        ["booking_date"] = payload.BookingDate.ToString("yyyy-MM-dd"),
        ["start_time"] = payload.StartTime.ToString("HH\\:mm"),
        ["end_time"] = payload.EndTime.ToString("HH\\:mm"),
        ["range_name"] = payload.RangeName,
        ["package_name"] = payload.PackageName,
        ["package_price"] = payload.PackagePrice,
        ["shooter_count"] = payload.ShooterCount,
        ["ics_url"] = payload.IcsUrl,
        ["google_calendar_url"] = payload.GoogleCalendarUrl,
        ["deposit_amount"] = payload.DepositAmount,
        ["deposit_due_at"] = payload.DepositDueAt,
        ["invoice_number"] = payload.InvoiceNumber,
        ["company"] = BookingRequestedNotifier.BuildCompanyProperties(payload.Company),
    };
}
