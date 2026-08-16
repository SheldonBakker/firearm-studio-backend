namespace FirearmStudio.Application.Model.Options;

public sealed class CustomerEngagementSettings
{
    public const string SectionName = nameof(CustomerEngagementSettings);

    public string ContactMetricName { get; init; } = "Contact Form Submitted";

    public string InvoiceSentMetricName { get; init; } = "Invoice Sent";

    public string BookingRequestedMetricName { get; init; } = "Booking Requested";

    public string BookingConfirmedMetricName { get; init; } = "Booking Confirmed";

    public string BookingReminderMetricName { get; init; } = "Booking Reminder";

    public string BookingCancelledMetricName { get; init; } = "Booking Cancelled";

    public string LicenceRenewalMetricName { get; init; } = "Licence Renewal Reminder";

    public string? ContactListId { get; init; }
}
