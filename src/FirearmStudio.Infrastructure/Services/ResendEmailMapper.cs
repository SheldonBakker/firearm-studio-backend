using FirearmStudio.Application.Abstractions.Email;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Options;

namespace FirearmStudio.Infrastructure.Services;

internal sealed record MappedEmail(
    string Alias,
    string To,
    string From,
    string? ReplyTo,
    Dictionary<string, object> Variables);

internal sealed class ResendEmailMapper(ResendSettings settings)
{
    internal MappedEmail? Map(EmailMessage message) => message switch
    {
        OtpEmail otp => MapOtp(otp),
        ContactFormReceivedEmail cf => MapContactFormReceived(cf),
        ContactFormAcknowledgementEmail ack => MapContactFormAcknowledgement(ack),
        InvoiceSentEmail inv => MapInvoiceSent(inv),
        BookingRequestedEmail br => MapBookingRequested(br),
        BookingLifecycleEmail bl => MapBookingLifecycle(bl),
        LicenceRenewalReminderEmail lr => MapLicenceRenewalReminder(lr),
        _ => throw new ArgumentOutOfRangeException(nameof(message), message.GetType().Name, "Unsupported email message type."),
    };

    private string PlatformFrom() => $"\"{settings.FromName}\" <{settings.FromAddress}>";

    private string TenantFrom(string? companyName) =>
        string.IsNullOrWhiteSpace(companyName)
            ? PlatformFrom()
            : $"\"{Sanitize(companyName, settings.FromName)} via {settings.FromName}\" <{settings.FromAddress}>";

    private static string Sanitize(string? value, string fallback = "-") =>
        string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Replace("<", "").Replace(">", "").Replace("\"", "");

    private static void AddCompanyVariables(Dictionary<string, object> vars, CompanyEmailDetails company)
    {
        vars["COMPANY_NAME"] = Sanitize(company.Name);
        vars["COMPANY_EMAIL"] = Sanitize(company.Email);
        vars["COMPANY_PHONE"] = Sanitize(company.Phone);
    }

    private static void AddBankVariables(Dictionary<string, object> vars, CompanyEmailDetails company)
    {
        vars["BANK_NAME"] = Sanitize(company.BankName);
        vars["BANK_ACCOUNT_HOLDER"] = Sanitize(company.BankAccountHolder);
        vars["BANK_ACCOUNT_NUMBER"] = Sanitize(company.BankAccountNumber);
        vars["BANK_BRANCH_CODE"] = Sanitize(company.BankBranchCode);
        vars["BANK_ACCOUNT_TYPE"] = Sanitize(company.BankAccountType);
    }

    private static string? ReplyToFor(CompanyEmailDetails company) =>
        string.IsNullOrWhiteSpace(company.Email) ? null : company.Email;

    private MappedEmail MapOtp(OtpEmail otp)
    {
        var alias = otp.Purpose switch
        {
            OtpPurpose.EmailConfirmation => "otp-signup-verification",
            OtpPurpose.PasswordReset => "otp-password-reset",
            OtpPurpose.Invite => "otp-team-invite",
            OtpPurpose.TwoFactor => "otp-login-verification",
            _ => throw new ArgumentOutOfRangeException(nameof(otp), otp.Purpose, null),
        };

        return new MappedEmail(
            Alias: alias,
            To: otp.Email,
            From: PlatformFrom(),
            ReplyTo: null,
            Variables: new Dictionary<string, object>
            {
                ["RECIPIENT_NAME"] = Sanitize(otp.Name, "there"),
                ["CODE"] = otp.Code ?? "",
                ["EXPIRES_MINUTES"] = otp.ExpiresInMinutes,
            });
    }

    private MappedEmail? MapContactFormReceived(ContactFormReceivedEmail cf)
    {
        if (string.IsNullOrWhiteSpace(settings.ContactInboxEmail))
        {
            return null;
        }

        var messageEncoded = System.Net.WebUtility.HtmlEncode(cf.Message).Replace("\n", "<br>");

        return new MappedEmail(
            Alias: "contact-form-received",
            To: settings.ContactInboxEmail,
            From: PlatformFrom(),
            ReplyTo: cf.SubmitterEmail,
            Variables: new Dictionary<string, object>
            {
                ["SUBMITTER_NAME"] = Sanitize(cf.SubmitterName),
                ["SUBMITTER_EMAIL"] = Sanitize(cf.SubmitterEmail),
                ["SUBMITTER_COMPANY"] = Sanitize(cf.SubmitterCompany),
                ["MESSAGE_HTML"] = messageEncoded,
                ["MESSAGE_TEXT"] = cf.Message ?? "",
            });
    }

    private MappedEmail MapContactFormAcknowledgement(ContactFormAcknowledgementEmail ack)
    {
        return new MappedEmail(
            Alias: "contact-form-acknowledgement",
            To: ack.RecipientEmail,
            From: PlatformFrom(),
            ReplyTo: string.IsNullOrWhiteSpace(settings.ContactInboxEmail) ? null : settings.ContactInboxEmail,
            Variables: new Dictionary<string, object>
            {
                ["RECIPIENT_NAME"] = Sanitize(ack.RecipientName, "there"),
            });
    }

    private MappedEmail MapInvoiceSent(InvoiceSentEmail inv)
    {
        var vars = new Dictionary<string, object>
        {
            ["CUSTOMER_NAME"] = Sanitize(inv.CustomerName, "there"),
            ["INVOICE_NUMBER"] = Sanitize(inv.InvoiceNumber),
            ["INVOICE_MONTH"] = ZarFormatting.FormatDate(inv.InvoiceMonth),
            ["DUE_ON"] = inv.DueOn.HasValue ? ZarFormatting.FormatDate(inv.DueOn.Value) : "-",
            ["SUBTOTAL"] = ZarFormatting.FormatMoney(inv.Subtotal),
            ["VAT_AMOUNT"] = ZarFormatting.FormatMoney(inv.VatAmount),
            ["TOTAL"] = ZarFormatting.FormatMoney(inv.Total),
            ["LINES_HTML"] = ResendEmailFragments.LinesHtml(inv.Lines),
            ["LINES_TEXT"] = ResendEmailFragments.LinesText(inv.Lines),
        };

        AddCompanyVariables(vars, inv.Company);
        AddBankVariables(vars, inv.Company);

        return new MappedEmail(
            Alias: "invoice-sent",
            To: inv.RecipientEmail,
            From: TenantFrom(inv.Company.Name),
            ReplyTo: ReplyToFor(inv.Company),
            Variables: vars);
    }

    private MappedEmail MapBookingRequested(BookingRequestedEmail br)
    {
        var vars = new Dictionary<string, object>
        {
            ["CUSTOMER_NAME"] = Sanitize(br.CustomerName, "there"),
            ["INVOICE_NUMBER"] = Sanitize(br.InvoiceNumber),
            ["SESSION_COUNT"] = br.Sessions.Count,
            ["SUBTOTAL"] = ZarFormatting.FormatMoney(br.Subtotal),
            ["VAT_AMOUNT"] = ZarFormatting.FormatMoney(br.VatAmount),
            ["TOTAL"] = ZarFormatting.FormatMoney(br.Total),
            ["SESSIONS_HTML"] = ResendEmailFragments.SessionsHtml(br.Sessions),
            ["SESSIONS_TEXT"] = ResendEmailFragments.SessionsText(br.Sessions),
        };

        AddCompanyVariables(vars, br.Company);
        AddBankVariables(vars, br.Company);

        return new MappedEmail(
            Alias: "booking-requested",
            To: br.RecipientEmail,
            From: TenantFrom(br.Company.Name),
            ReplyTo: ReplyToFor(br.Company),
            Variables: vars);
    }

    private MappedEmail MapBookingLifecycle(BookingLifecycleEmail bl)
    {
        var alias = bl.Kind switch
        {
            BookingLifecycleKind.Confirmed => "booking-confirmed",
            BookingLifecycleKind.Reminder => "booking-reminder",
            BookingLifecycleKind.Cancelled => "booking-cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(bl), bl.Kind, null),
        };

        var session = bl.Session;

        var vars = new Dictionary<string, object>
        {
            ["CUSTOMER_NAME"] = Sanitize(bl.CustomerName, "there"),
            ["BOOKING_NUMBER"] = Sanitize(session.BookingNumber),
            ["BOOKING_DATE"] = ZarFormatting.FormatDate(session.Date),
            ["START_TIME"] = ZarFormatting.FormatTime(session.Start),
            ["END_TIME"] = ZarFormatting.FormatTime(session.End),
            ["RANGE_NAME"] = Sanitize(session.RangeName),
            ["PACKAGE_NAME"] = Sanitize(session.PackageName),
        };

        AddCompanyVariables(vars, bl.Company);

        if (bl.Kind != BookingLifecycleKind.Cancelled)
        {
            vars["PACKAGE_PRICE"] = ZarFormatting.FormatMoney(session.PackagePrice);
            vars["SHOOTER_COUNT"] = bl.ShooterCount;
            vars["CALENDAR_HTML"] = ResendEmailFragments.CalendarHtml(session.IcsUrl, session.GoogleCalendarUrl);
            vars["CALENDAR_TEXT"] = ResendEmailFragments.CalendarText(session.IcsUrl, session.GoogleCalendarUrl);
            vars["DEPOSIT_AMOUNT"] = session.DepositAmount.HasValue
                ? ZarFormatting.FormatMoney(session.DepositAmount.Value)
                : "-";
            vars["DEPOSIT_DUE"] = session.DepositDueAt.HasValue
                ? ZarFormatting.FormatDateTime(session.DepositDueAt.Value)
                : "-";
            vars["INVOICE_NUMBER"] = Sanitize(bl.InvoiceNumber);
        }

        return new MappedEmail(
            Alias: alias,
            To: bl.RecipientEmail,
            From: TenantFrom(bl.Company.Name),
            ReplyTo: ReplyToFor(bl.Company),
            Variables: vars);
    }

    private MappedEmail MapLicenceRenewalReminder(LicenceRenewalReminderEmail lr)
    {
        var vars = new Dictionary<string, object>
        {
            ["CUSTOMER_NAME"] = Sanitize(lr.CustomerName, "there"),
            ["LICENCE_NUMBER"] = Sanitize(lr.LicenceNumber),
            ["EXPIRES_ON"] = ZarFormatting.FormatDate(lr.ExpiresOn),
            ["DAYS_UNTIL_EXPIRY"] = lr.DaysUntilExpiry,
            ["TIER"] = Sanitize(lr.Tier),
            ["FIREARM_MAKE"] = Sanitize(lr.FirearmMake),
            ["FIREARM_MODEL"] = Sanitize(lr.FirearmModel),
            ["SERIAL_NUMBER"] = Sanitize(lr.SerialNumber),
        };

        AddCompanyVariables(vars, lr.Company);

        return new MappedEmail(
            Alias: "licence-renewal-reminder",
            To: lr.RecipientEmail,
            From: TenantFrom(lr.Company.Name),
            ReplyTo: ReplyToFor(lr.Company),
            Variables: vars);
    }
}
