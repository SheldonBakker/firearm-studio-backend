using System.Net;
using System.Text;
using FirearmStudio.Application.Abstractions.Email;

namespace FirearmStudio.Infrastructure.Services;

internal static class ResendEmailFragments
{
    internal static string LinesHtml(IReadOnlyList<InvoiceEmailLine> lines)
    {
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            sb.Append("<tr>");
            sb.Append($"<td>{WebUtility.HtmlEncode(line.Description)}</td>");
            sb.Append($"<td>{line.Quantity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}</td>");
            sb.Append($"<td>{WebUtility.HtmlEncode(ZarFormatting.FormatMoney(line.UnitPrice))}</td>");
            sb.Append($"<td>{WebUtility.HtmlEncode(ZarFormatting.FormatMoney(line.LineTotal))}</td>");
            sb.Append("</tr>");
        }

        return sb.ToString();
    }

    internal static string LinesText(IReadOnlyList<InvoiceEmailLine> lines)
    {
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            sb.AppendLine(
                $"{line.Description} " +
                $"x{line.Quantity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} " +
                $"@ {ZarFormatting.FormatMoney(line.UnitPrice)} " +
                $"= {ZarFormatting.FormatMoney(line.LineTotal)}");
        }

        return sb.ToString().TrimEnd();
    }

    internal static string SessionsHtml(IReadOnlyList<BookingEmailSession> sessions)
    {
        var sb = new StringBuilder();
        foreach (var session in sessions)
        {
            sb.Append("<div>");
            sb.Append($"<p><strong>Booking {WebUtility.HtmlEncode(session.BookingNumber)}</strong></p>");
            sb.Append($"<p>Date: {WebUtility.HtmlEncode(ZarFormatting.FormatDate(session.Date))}</p>");
            sb.Append($"<p>Time: {WebUtility.HtmlEncode(ZarFormatting.FormatTime(session.Start))} - {WebUtility.HtmlEncode(ZarFormatting.FormatTime(session.End))}</p>");

            if (!string.IsNullOrWhiteSpace(session.RangeName))
            {
                sb.Append($"<p>Range: {WebUtility.HtmlEncode(session.RangeName)}</p>");
            }

            sb.Append($"<p>Package: {WebUtility.HtmlEncode(session.PackageName)} - {WebUtility.HtmlEncode(ZarFormatting.FormatMoney(session.PackagePrice))}</p>");

            if (session.DepositAmount.HasValue)
            {
                sb.Append($"<p>Deposit: {WebUtility.HtmlEncode(ZarFormatting.FormatMoney(session.DepositAmount.Value))}");

                if (session.DepositDueAt.HasValue)
                {
                    sb.Append($" due {WebUtility.HtmlEncode(ZarFormatting.FormatDateTime(session.DepositDueAt.Value))}");
                }

                sb.Append("</p>");
            }

            sb.Append(CalendarHtml(session.IcsUrl, session.GoogleCalendarUrl));
            sb.Append("</div>");
        }

        return sb.ToString();
    }

    internal static string SessionsText(IReadOnlyList<BookingEmailSession> sessions)
    {
        var sb = new StringBuilder();
        foreach (var session in sessions)
        {
            sb.AppendLine($"Booking {session.BookingNumber}");
            sb.AppendLine($"Date: {ZarFormatting.FormatDate(session.Date)}");
            sb.AppendLine($"Time: {ZarFormatting.FormatTime(session.Start)} - {ZarFormatting.FormatTime(session.End)}");

            if (!string.IsNullOrWhiteSpace(session.RangeName))
            {
                sb.AppendLine($"Range: {session.RangeName}");
            }

            sb.AppendLine($"Package: {session.PackageName} - {ZarFormatting.FormatMoney(session.PackagePrice)}");

            if (session.DepositAmount.HasValue)
            {
                var depositLine = $"Deposit: {ZarFormatting.FormatMoney(session.DepositAmount.Value)}";

                if (session.DepositDueAt.HasValue)
                {
                    depositLine += $" due {ZarFormatting.FormatDateTime(session.DepositDueAt.Value)}";
                }

                sb.AppendLine(depositLine);
            }

            var calText = CalendarText(session.IcsUrl, session.GoogleCalendarUrl);
            if (!string.IsNullOrEmpty(calText))
            {
                sb.AppendLine(calText);
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    internal static string CalendarHtml(string? icsUrl, string? googleCalendarUrl)
    {
        if (string.IsNullOrEmpty(icsUrl) && string.IsNullOrEmpty(googleCalendarUrl))
        {
            return "";
        }

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(icsUrl))
        {
            sb.Append(
                $"<a href=\"{WebUtility.HtmlEncode(icsUrl)}\" " +
                "style=\"display:inline-block;padding:8px 16px;background:#000000;color:#ffffff;text-decoration:none;border-radius:4px;\">" +
                "Add to Calendar (ICS)</a>");
        }

        if (!string.IsNullOrEmpty(googleCalendarUrl))
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(
                $"<a href=\"{WebUtility.HtmlEncode(googleCalendarUrl)}\" " +
                "style=\"display:inline-block;padding:8px 16px;background:#4285f4;color:#ffffff;text-decoration:none;border-radius:4px;\">" +
                "Add to Google Calendar</a>");
        }

        return sb.ToString();
    }

    internal static string CalendarText(string? icsUrl, string? googleCalendarUrl)
    {
        if (string.IsNullOrEmpty(icsUrl) && string.IsNullOrEmpty(googleCalendarUrl))
        {
            return "";
        }

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(icsUrl))
        {
            sb.AppendLine($"Add to Calendar (ICS): {icsUrl}");
        }

        if (!string.IsNullOrEmpty(googleCalendarUrl))
        {
            sb.AppendLine($"Add to Google Calendar: {googleCalendarUrl}");
        }

        return sb.ToString().TrimEnd();
    }
}
