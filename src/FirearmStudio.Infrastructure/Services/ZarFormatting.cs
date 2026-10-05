using System.Globalization;

namespace FirearmStudio.Infrastructure.Services;

internal static class ZarFormatting
{
    private static readonly NumberFormatInfo MoneyFormat = BuildMoneyFormat();
    private static readonly TimeZoneInfo JohannesburgTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");

    private static NumberFormatInfo BuildMoneyFormat()
    {
        var fmt = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        fmt.CurrencySymbol = "R ";
        fmt.CurrencyGroupSeparator = " ";
        fmt.CurrencyDecimalSeparator = ".";
        fmt.CurrencyDecimalDigits = 2;
        fmt.CurrencyPositivePattern = 0;
        return fmt;
    }

    internal static string FormatMoney(decimal amount) =>
        amount.ToString("C", MoneyFormat);

    internal static string FormatDate(DateOnly date) =>
        date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    internal static string FormatTime(TimeOnly time) =>
        time.ToString("HH:mm", CultureInfo.InvariantCulture);

    internal static string FormatDateTime(DateTime dt)
    {
        var utc = dt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            : dt.ToUniversalTime();
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, JohannesburgTimeZone);
        return local.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}
