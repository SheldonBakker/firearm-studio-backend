using System.Net;
using System.Text.Json;
using FirearmStudio.Application.Abstractions.Email;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Options;
using FirearmStudio.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class ResendEmailSenderTests
{
    private sealed class CapturingHandler(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string responseBody = "{}") : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody),
            };
        }
    }

    private static ResendSettings DefaultSettings() => new()
    {
        ApiKey = "test-api-key",
        BaseUrl = "https://api.resend.com",
        FromAddress = "no-reply@firearmstudio.com",
        FromName = "Firearm Studio",
        ContactInboxEmail = "hello@firearmstudio.com",
        ContactSegmentId = "segment-123",
    };

    private static (ResendEmailSender Sender, CapturingHandler Handler) Build(
        ResendSettings? settings = null,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string responseBody = "{}")
    {
        settings ??= DefaultSettings();
        var handler = new CapturingHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/"),
        };
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "Authorization", $"Bearer {settings.ApiKey}");
        var mapper = new ResendEmailMapper(settings);
        return (new ResendEmailSender(httpClient, mapper, NullLogger<ResendEmailSender>.Instance), handler);
    }

    private static JsonElement ParseBody(string? body) =>
        JsonDocument.Parse(body!).RootElement;

    private static CompanyEmailDetails SampleCompany(string? name = "Test Range") =>
        new(name, "range@example.com", "+27211234567",
            "FNB", "Test Range Pty Ltd", "12345678", "250655", "Current");

    [Fact]
    public async Task Send_posts_to_emails_path()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new OtpEmail(OtpPurpose.EmailConfirmation, "user@example.com", "User", "123456", 15),
            default);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("emails", handler.LastRequest!.RequestUri!.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task Send_includes_bearer_auth_header()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new OtpEmail(OtpPurpose.TwoFactor, "user@example.com", null, "654321", 15),
            default);

        Assert.True(handler.LastRequest!.Headers.TryGetValues("Authorization", out var values));
        Assert.Contains("Bearer test-api-key", values);
    }

    [Theory]
    [InlineData(OtpPurpose.EmailConfirmation, "otp-signup-verification")]
    [InlineData(OtpPurpose.PasswordReset, "otp-password-reset")]
    [InlineData(OtpPurpose.Invite, "otp-team-invite")]
    [InlineData(OtpPurpose.TwoFactor, "otp-login-verification")]
    public async Task Each_OtpPurpose_maps_to_its_alias(OtpPurpose purpose, string expectedAlias)
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new OtpEmail(purpose, "user@example.com", "User", "123456", 15),
            default);

        var body = ParseBody(handler.LastRequestBody);
        var alias = body.GetProperty("template").GetProperty("id").GetString();
        Assert.Equal(expectedAlias, alias);
    }

    [Fact]
    public async Task Every_OtpPurpose_maps_to_a_unique_non_empty_alias()
    {
        var (sender, handler) = Build();
        var aliases = new HashSet<string>();

        foreach (var purpose in Enum.GetValues<OtpPurpose>())
        {
            await sender.SendAsync(
                new OtpEmail(purpose, "user@example.com", null, "111111", 15),
                default);

            var body = ParseBody(handler.LastRequestBody);
            var alias = body.GetProperty("template").GetProperty("id").GetString()!;
            Assert.False(string.IsNullOrWhiteSpace(alias));
            aliases.Add(alias);
        }

        Assert.Equal(Enum.GetValues<OtpPurpose>().Length, aliases.Count);
    }

    [Fact]
    public async Task Platform_email_from_is_Firearm_Studio()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new OtpEmail(OtpPurpose.EmailConfirmation, "user@example.com", null, "123456", 15),
            default);

        var body = ParseBody(handler.LastRequestBody);
        Assert.Equal("Firearm Studio <no-reply@firearmstudio.com>", body.GetProperty("from").GetString());
    }

    [Fact]
    public async Task Tenant_email_from_includes_company_name_via_Firearm_Studio()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new InvoiceSentEmail(
                "customer@example.com", "Customer",
                "INV-001", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31),
                1000m, 150m, 1150m,
                [new InvoiceEmailLine("Session", 1, 1000m, 1000m)],
                SampleCompany("Shooting Range SA"))
            { IdempotencyKey = "invoice-sent:abc:2026-10-01T00:00:00Z" },
            default);

        var body = ParseBody(handler.LastRequestBody);
        Assert.Equal(
            "Shooting Range SA via Firearm Studio <no-reply@firearmstudio.com>",
            body.GetProperty("from").GetString());
    }

    [Fact]
    public async Task Tenant_email_from_falls_back_to_Firearm_Studio_when_company_name_is_null()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new InvoiceSentEmail(
                "customer@example.com", null,
                "INV-002", new DateOnly(2026, 10, 1), null,
                0m, 0m, 0m,
                [],
                SampleCompany(null))
            { IdempotencyKey = "invoice-sent:abc:2026-10-01T00:00:00Z" },
            default);

        var body = ParseBody(handler.LastRequestBody);
        Assert.Equal("Firearm Studio <no-reply@firearmstudio.com>", body.GetProperty("from").GetString());
    }

    [Fact]
    public async Task Invoice_email_has_company_reply_to()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new InvoiceSentEmail(
                "customer@example.com", "Customer",
                "INV-001", new DateOnly(2026, 10, 1), null,
                100m, 15m, 115m,
                [],
                SampleCompany())
            { IdempotencyKey = "invoice-sent:abc:2026-10-01T00:00:00Z" },
            default);

        var body = ParseBody(handler.LastRequestBody);
        Assert.Equal("range@example.com", body.GetProperty("reply_to").GetString());
    }

    [Fact]
    public async Task Otp_email_has_no_reply_to()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new OtpEmail(OtpPurpose.TwoFactor, "user@example.com", null, "123456", 15),
            default);

        var body = ParseBody(handler.LastRequestBody);
        Assert.False(body.TryGetProperty("reply_to", out _));
    }

    [Fact]
    public async Task Money_is_formatted_as_R_1_234_56()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new InvoiceSentEmail(
                "customer@example.com", "Customer",
                "INV-001", new DateOnly(2026, 10, 1), null,
                1234.56m, 185.18m, 1419.74m,
                [new InvoiceEmailLine("Session", 1, 1234.56m, 1234.56m)],
                SampleCompany())
            { IdempotencyKey = "invoice-sent:abc:2026-10-01T00:00:00Z" },
            default);

        var vars = ParseBody(handler.LastRequestBody)
            .GetProperty("template").GetProperty("variables");
        Assert.Equal("R 1 234.56", vars.GetProperty("SUBTOTAL").GetString());
    }

    [Fact]
    public async Task Date_is_formatted_dd_MMM_yyyy()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new InvoiceSentEmail(
                "customer@example.com", "Customer",
                "INV-001", new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 31),
                100m, 15m, 115m,
                [],
                SampleCompany())
            { IdempotencyKey = "invoice-sent:abc:2026-10-01T00:00:00Z" },
            default);

        var vars = ParseBody(handler.LastRequestBody)
            .GetProperty("template").GetProperty("variables");
        Assert.Equal("05 Oct 2026", vars.GetProperty("INVOICE_MONTH").GetString());
        Assert.Equal("31 Oct 2026", vars.GetProperty("DUE_ON").GetString());
    }

    [Fact]
    public async Task Scalar_ampersand_is_not_encoded_in_company_name()
    {
        var (sender, handler) = Build();
        var company = new CompanyEmailDetails(
            "A & B Range", "ab@example.com", null,
            null, null, null, null, null);

        await sender.SendAsync(
            new InvoiceSentEmail(
                "customer@example.com", "Customer",
                "INV-001", new DateOnly(2026, 10, 1), null,
                100m, 15m, 115m,
                [],
                company)
            { IdempotencyKey = "invoice-sent:abc:2026-10-01T00:00:00Z" },
            default);

        var vars = ParseBody(handler.LastRequestBody)
            .GetProperty("template").GetProperty("variables");
        Assert.Equal("A & B Range", vars.GetProperty("COMPANY_NAME").GetString());
    }

    [Fact]
    public async Task Angle_brackets_are_stripped_from_scalar_values()
    {
        var (sender, handler) = Build();
        var company = new CompanyEmailDetails(
            "<b>Range</b>", null, null,
            null, null, null, null, null);

        await sender.SendAsync(
            new InvoiceSentEmail(
                "customer@example.com", "Customer",
                "INV-001", new DateOnly(2026, 10, 1), null,
                100m, 15m, 115m,
                [],
                company)
            { IdempotencyKey = "invoice-sent:abc:2026-10-01T00:00:00Z" },
            default);

        var vars = ParseBody(handler.LastRequestBody)
            .GetProperty("template").GetProperty("variables");
        Assert.Equal("bRange/b", vars.GetProperty("COMPANY_NAME").GetString());
    }

    [Fact]
    public async Task Lines_html_html_encodes_descriptions_and_text_uses_raw()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new InvoiceSentEmail(
                "customer@example.com", "Customer",
                "INV-001", new DateOnly(2026, 10, 1), null,
                100m, 15m, 115m,
                [new InvoiceEmailLine("A & B session", 1, 100m, 100m)],
                SampleCompany())
            { IdempotencyKey = "invoice-sent:abc:2026-10-01T00:00:00Z" },
            default);

        var vars = ParseBody(handler.LastRequestBody)
            .GetProperty("template").GetProperty("variables");

        var linesHtml = vars.GetProperty("LINES_HTML").GetString()!;
        var linesText = vars.GetProperty("LINES_TEXT").GetString()!;

        Assert.Contains("A &amp; B session", linesHtml);
        Assert.Contains("A & B session", linesText);
        Assert.DoesNotContain("A &amp; B session", linesText);
    }

    [Fact]
    public async Task Sessions_html_and_text_are_present_for_booking_requested()
    {
        var (sender, handler) = Build();
        var session = new BookingEmailSession(
            "BK-001", new DateOnly(2026, 10, 5),
            new TimeOnly(9, 0), new TimeOnly(11, 0),
            "Main Range", "Standard Package", 500m,
            null, null, null, null);

        await sender.SendAsync(
            new BookingRequestedEmail(
                "customer@example.com", "Customer",
                "INV-001", 500m, 75m, 575m,
                [session],
                SampleCompany())
            { IdempotencyKey = "outbox:session-abc" },
            default);

        var vars = ParseBody(handler.LastRequestBody)
            .GetProperty("template").GetProperty("variables");

        Assert.False(string.IsNullOrEmpty(vars.GetProperty("SESSIONS_HTML").GetString()));
        Assert.False(string.IsNullOrEmpty(vars.GetProperty("SESSIONS_TEXT").GetString()));
        Assert.Equal("booking-requested",
            ParseBody(handler.LastRequestBody).GetProperty("template").GetProperty("id").GetString());
    }

    [Fact]
    public async Task Calendar_html_present_for_booking_confirmed_with_urls()
    {
        var (sender, handler) = Build();
        var session = new BookingEmailSession(
            "BK-001", new DateOnly(2026, 10, 5),
            new TimeOnly(9, 0), new TimeOnly(11, 0),
            null, "Standard Package", 500m,
            "https://example.com/cal.ics", "https://calendar.google.com/add",
            250m, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));

        await sender.SendAsync(
            new BookingLifecycleEmail(
                BookingLifecycleKind.Confirmed, "customer@example.com", "Customer",
                session, 2, "INV-001", SampleCompany())
            { IdempotencyKey = "outbox:lifecycle-abc" },
            default);

        var vars = ParseBody(handler.LastRequestBody)
            .GetProperty("template").GetProperty("variables");

        var calHtml = vars.GetProperty("CALENDAR_HTML").GetString()!;
        Assert.Contains("href=", calHtml);
        Assert.Contains("Add to Calendar", calHtml);
    }

    [Fact]
    public async Task Idempotency_key_is_present_for_invoice_and_outbox_sends()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new InvoiceSentEmail(
                "customer@example.com", "Customer",
                "INV-001", new DateOnly(2026, 10, 1), null,
                100m, 15m, 115m,
                [],
                SampleCompany())
            { IdempotencyKey = "invoice-sent:abc:2026-10-01T00:00:00Z" },
            default);

        Assert.True(handler.LastRequest!.Headers.TryGetValues("Idempotency-Key", out var values));
        Assert.Contains("invoice-sent:abc:2026-10-01T00:00:00Z", values);
    }

    [Fact]
    public async Task Idempotency_key_is_absent_for_otp_sends()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new OtpEmail(OtpPurpose.TwoFactor, "user@example.com", null, "123456", 15),
            default);

        Assert.False(handler.LastRequest!.Headers.TryGetValues("Idempotency-Key", out _));
    }

    [Fact]
    public async Task Non_2xx_throws_with_status_and_body()
    {
        var (sender, _) = Build(
            statusCode: HttpStatusCode.UnprocessableEntity,
            responseBody: "{\"message\":\"template not found\"}");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sender.SendAsync(
                new OtpEmail(OtpPurpose.PasswordReset, "user@example.com", null, "999888", 15),
                default));

        Assert.Contains("422", ex.Message);
        Assert.Contains("template not found", ex.Message);
    }

    [Fact]
    public async Task Non_2xx_exception_does_not_contain_the_code()
    {
        var (sender, _) = Build(
            statusCode: HttpStatusCode.BadRequest,
            responseBody: "{\"error\":\"bad request\"}");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sender.SendAsync(
                new OtpEmail(OtpPurpose.EmailConfirmation, "user@example.com", null, "SECRET_CODE_777", 15),
                default));

        Assert.DoesNotContain("SECRET_CODE_777", ex.Message);
    }

    [Fact]
    public async Task Contact_form_received_sends_to_inbox_email_with_submitter_as_reply_to()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new ContactFormReceivedEmail(
                "John Doe", "john@example.com", "ACME Corp", "Hello there"),
            default);

        var body = ParseBody(handler.LastRequestBody);
        var to = body.GetProperty("to")[0].GetString();
        Assert.Equal("hello@firearmstudio.com", to);
        Assert.Equal("john@example.com", body.GetProperty("reply_to").GetString());
        Assert.Equal("contact-form-received",
            body.GetProperty("template").GetProperty("id").GetString());
    }

    [Fact]
    public async Task Contact_form_received_no_ops_when_inbox_email_is_blank()
    {
        var settings = new ResendSettings
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.resend.com",
            FromAddress = "no-reply@firearmstudio.com",
            FromName = "Firearm Studio",
            ContactInboxEmail = "",
            ContactSegmentId = "segment-123",
        };
        var (sender, handler) = Build(settings);

        await sender.SendAsync(
            new ContactFormReceivedEmail("Jane", "jane@example.com", null, "Hi"),
            default);

        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task Contact_form_acknowledgement_has_inbox_as_reply_to_when_set()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new ContactFormAcknowledgementEmail("user@example.com", "User"),
            default);

        var body = ParseBody(handler.LastRequestBody);
        Assert.Equal("hello@firearmstudio.com", body.GetProperty("reply_to").GetString());
        Assert.Equal("contact-form-acknowledgement",
            body.GetProperty("template").GetProperty("id").GetString());
    }

    [Fact]
    public async Task Otp_variables_contain_code_and_expiry()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new OtpEmail(OtpPurpose.EmailConfirmation, "user@example.com", "Alice", "112233", 30),
            default);

        var vars = ParseBody(handler.LastRequestBody)
            .GetProperty("template").GetProperty("variables");
        Assert.Equal("112233", vars.GetProperty("CODE").GetString());
        Assert.Equal(30, vars.GetProperty("EXPIRES_MINUTES").GetInt32());
        Assert.Equal("Alice", vars.GetProperty("RECIPIENT_NAME").GetString());
    }

    [Fact]
    public async Task Licence_renewal_reminder_maps_to_correct_alias()
    {
        var (sender, handler) = Build();

        await sender.SendAsync(
            new LicenceRenewalReminderEmail(
                "owner@example.com", "Owner",
                "LIC-001", new DateOnly(2026, 12, 31), 87, "A",
                "Glock", "17", "SN123",
                SampleCompany())
            { IdempotencyKey = "outbox:licence-abc" },
            default);

        var body = ParseBody(handler.LastRequestBody);
        Assert.Equal("licence-renewal-reminder",
            body.GetProperty("template").GetProperty("id").GetString());
    }
}
