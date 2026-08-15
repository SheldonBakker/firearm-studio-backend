using System.Net;
using System.Text.Json;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Services;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class WahaWhatsAppSenderTests
{
    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status);
        }
    }

    private static (WahaWhatsAppSender Sender, StubHandler Handler) Build(HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new StubHandler(status);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://waha.test/") };
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", "k");
        var settings = new WahaSettings { SessionId = "sess-123", ApiKey = "k" };
        return (new WahaWhatsAppSender(client, settings), handler);
    }

    private static string ChatId(string body) =>
        JsonDocument.Parse(body).RootElement.GetProperty("chatId").GetString()!;

    private static string Text(string body) =>
        JsonDocument.Parse(body).RootElement.GetProperty("text").GetString()!;

    [Fact]
    public async Task Formats_chat_id_as_digits_at_c_us()
    {
        var (sender, handler) = Build();
        await sender.SendOtpAsync("+27821234567", OtpPurpose.EmailConfirmation, "123456", 15, default);
        Assert.Equal("27821234567@c.us", ChatId(handler.Body!));
    }

    [Fact]
    public async Task Posts_to_the_session_send_text_path()
    {
        var (sender, handler) = Build();
        await sender.SendOtpAsync("+27821234567", OtpPurpose.EmailConfirmation, "123456", 15, default);
        Assert.EndsWith("/api/sessions/sess-123/messages/send-text", handler.Request!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Sends_the_api_key_header()
    {
        var (sender, handler) = Build();
        await sender.SendOtpAsync("+27821234567", OtpPurpose.EmailConfirmation, "123456", 15, default);
        Assert.True(handler.Request!.Headers.TryGetValues("X-API-Key", out var values));
        Assert.Equal("k", Assert.Single(values!));
    }

    [Fact]
    public async Task Text_contains_the_code_and_expiry()
    {
        var (sender, handler) = Build();
        await sender.SendOtpAsync("+27821234567", OtpPurpose.EmailConfirmation, "123456", 15, default);
        var text = Text(handler.Body!);
        Assert.Contains("123456", text);
        Assert.Contains("15 minutes", text);
    }

    [Fact]
    public async Task Throws_on_non_success_status()
    {
        var (sender, _) = Build(HttpStatusCode.InternalServerError);
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sender.SendOtpAsync("+27821234567", OtpPurpose.EmailConfirmation, "123456", 15, default));
    }

    [Theory]
    [InlineData(OtpPurpose.EmailConfirmation)]
    [InlineData(OtpPurpose.PasswordReset)]
    [InlineData(OtpPurpose.Invite)]
    [InlineData(OtpPurpose.TwoFactor)]
    [InlineData(OtpPurpose.PhoneChange)]
    public async Task Every_purpose_produces_a_message(OtpPurpose purpose)
    {
        var (sender, handler) = Build();
        await sender.SendOtpAsync("+27821234567", purpose, "123456", 15, default);
        Assert.False(string.IsNullOrWhiteSpace(Text(handler.Body!)));
    }
}
