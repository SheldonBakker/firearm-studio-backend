using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Bookings;

/// <summary>
/// Fires the customer-facing "Booking Requested" Klaviyo event after a public booking
/// (single or cart) is committed. Best-effort: any Klaviyo failure is logged and swallowed
/// so it never fails the booking, and must be called AFTER the booking transaction commits.
/// </summary>
internal static class BookingRequestedNotifier
{
    internal static async Task NotifyAsync(
        IKlaviyoClient klaviyo,
        KlaviyoSettings settings,
        ILogger logger,
        string email,
        string? name,
        IReadOnlyDictionary<string, object?> properties,
        string logContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning(
                "Skipped Klaviyo booking-requested event ({LogContext}): customer has no email.",
                logContext);
            return;
        }

        try
        {
            await klaviyo.TrackEventAsync(
                settings.BookingRequestedMetricName,
                email,
                name,
                properties,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send booking-requested event to Klaviyo ({LogContext}).",
                logContext);
        }
    }

    internal static Dictionary<string, object?> BuildCompanyProperties(Company? company)
    {
        if (company is null)
        {
            return [];
        }

        return new Dictionary<string, object?>
        {
            ["id"] = company.Id,
            ["name"] = company.Name,
            ["email"] = company.Email,
            ["phone"] = company.Phone,
            ["address_line1"] = company.AddressLine1,
            ["address_line2"] = company.AddressLine2,
            ["city"] = company.City,
            ["province"] = company.Province,
            ["postal_code"] = company.PostalCode,
        };
    }
}
