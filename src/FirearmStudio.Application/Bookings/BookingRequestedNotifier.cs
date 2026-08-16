using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Bookings;

internal static class BookingRequestedNotifier
{
    internal static async Task SendAsync(
        ICustomerEngagementClient engagement,
        CustomerEngagementSettings settings,
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
                "Skipped booking-requested engagement event ({LogContext}): customer has no email.",
                logContext);
            return;
        }

        await engagement.TrackEventAsync(
            settings.BookingRequestedMetricName,
            email,
            name,
            Flatten(properties),
            cancellationToken);
    }

    internal static Dictionary<string, object?> BuildProperties(BookingRequestedPayload payload)
    {
        var response = payload.Response;
        var detailsByBookingId = payload.BookingDetails.ToDictionary(detail => detail.BookingId);

        return new Dictionary<string, object?>
        {
            ["invoice_id"] = response.InvoiceId,
            ["invoice_number"] = response.InvoiceNumber,
            ["session_count"] = response.Bookings.Count,
            ["subtotal"] = response.Subtotal,
            ["vat_amount"] = response.VatAmount,
            ["total"] = response.Total,
            ["bookings"] = response.Bookings
                .Select(booking =>
                {
                    detailsByBookingId.TryGetValue(booking.Id, out var detail);

                    return new Dictionary<string, object?>
                    {
                        ["booking_id"] = booking.Id,
                        ["booking_number"] = booking.BookingNumber,
                        ["status"] = booking.Status.ToString(),
                        ["booking_date"] = booking.BookingDate.ToString("yyyy-MM-dd"),
                        ["start_time"] = booking.StartTime.ToString("HH\\:mm"),
                        ["end_time"] = booking.EndTime.ToString("HH\\:mm"),
                        ["range_name"] = booking.RangeName,
                        ["package_name"] = booking.PackageName,
                        ["package_price"] = booking.PackagePrice,
                        ["ics_url"] = detail?.IcsUrl,
                        ["google_calendar_url"] = detail?.GoogleCalendarUrl,
                        ["deposit_amount"] = detail?.DepositAmount,
                        ["deposit_due_at"] = detail?.DepositDueAt,
                    };
                })
                .ToList(),
            ["company"] = BuildCompanyProperties(payload.Company),
        };
    }

    internal static Dictionary<string, object?> Flatten(
        IReadOnlyDictionary<string, object?> properties, string separator = "_")
    {
        var result = new Dictionary<string, object?>();
        FlattenInto(result, prefix: null, properties, separator);
        return result;
    }

    private static void FlattenInto(
        Dictionary<string, object?> target,
        string? prefix,
        IReadOnlyDictionary<string, object?> source,
        string separator)
    {
        foreach (var (key, value) in source)
        {
            var compositeKey = prefix is null ? key : $"{prefix}{separator}{key}";

            if (value is IReadOnlyDictionary<string, object?> nested)
            {
                FlattenInto(target, compositeKey, nested, separator);
            }
            else
            {
                target[compositeKey] = value;
            }
        }
    }

    internal static Dictionary<string, object?> BuildCompanyProperties(CompanyNotificationData? company)
    {
        if (company is null)
        {
            return [];
        }

        return new Dictionary<string, object?>
        {
            ["id"] = company.Id,
            ["name"] = company.Name,
            ["registration_number"] = company.RegistrationNumber,
            ["vat_number"] = company.VatNumber,
            ["email"] = company.Email,
            ["phone"] = company.Phone,
            ["address_line1"] = company.AddressLine1,
            ["address_line2"] = company.AddressLine2,
            ["city"] = company.City,
            ["province"] = company.Province,
            ["postal_code"] = company.PostalCode,
            ["bank_name"] = company.BankName,
            ["bank_account_holder"] = company.BankAccountHolder,
            ["bank_account_number"] = company.BankAccountNumber,
            ["bank_branch_code"] = company.BankBranchCode,
            ["bank_account_type"] = company.BankAccountType,
            ["bank_swift_code"] = company.BankSwiftCode,
        };
    }
}
