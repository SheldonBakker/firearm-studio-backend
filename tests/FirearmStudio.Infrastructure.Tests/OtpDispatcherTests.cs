using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class OtpDispatcherTests
{
    private sealed class RecordingEmailSender(bool throws = false) : IEmailSender
    {
        public int Calls { get; private set; }
        public string? LastEmail { get; private set; }

        public Task SendOtpAsync(string email, string? name, OtpPurpose purpose, string code, int expiresInMinutes, CancellationToken ct)
        {
            Calls++;
            LastEmail = email;
            if (throws)
            {
                throw new InvalidOperationException("email down");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class RecordingWhatsAppSender(bool throws = false) : IWhatsAppSender
    {
        public int Calls { get; private set; }
        public string? LastPhone { get; private set; }

        public Task SendOtpAsync(string phoneE164, OtpPurpose purpose, string code, int expiresInMinutes, CancellationToken ct)
        {
            Calls++;
            LastPhone = phoneE164;
            if (throws)
            {
                throw new HttpRequestException("waha down");
            }

            return Task.CompletedTask;
        }
    }

    private static OtpDispatcher Build(RecordingEmailSender email, RecordingWhatsAppSender whatsApp) =>
        new(email, whatsApp, NullLogger<OtpDispatcher>.Instance);

    [Fact]
    public async Task EmailConfirmation_sends_both_channels()
    {
        var email = new RecordingEmailSender();
        var whatsApp = new RecordingWhatsAppSender();
        await Build(email, whatsApp).SendAsync(
            new OtpRecipient("user@example.com", null, "+27821234567"),
            OtpPurpose.EmailConfirmation, "123456", 15, default);

        Assert.Equal(1, email.Calls);
        Assert.Equal(1, whatsApp.Calls);
    }

    [Fact]
    public async Task Throwing_whatsapp_does_not_fail_email_confirmation()
    {
        var email = new RecordingEmailSender();
        var whatsApp = new RecordingWhatsAppSender(throws: true);
        await Build(email, whatsApp).SendAsync(
            new OtpRecipient("user@example.com", null, "+27821234567"),
            OtpPurpose.EmailConfirmation, "123456", 15, default);

        Assert.Equal(1, email.Calls);
    }

    [Fact]
    public async Task Throwing_whatsapp_does_not_fail_phone_change()
    {
        var email = new RecordingEmailSender();
        var whatsApp = new RecordingWhatsAppSender(throws: true);
        await Build(email, whatsApp).SendAsync(
            new OtpRecipient("user@example.com", null, "+27820000002"),
            OtpPurpose.PhoneChange, "123456", 15, default);

        Assert.Equal(1, email.Calls);
    }

    [Theory]
    [InlineData(OtpPurpose.EmailConfirmation)]
    [InlineData(OtpPurpose.PasswordReset)]
    [InlineData(OtpPurpose.Invite)]
    [InlineData(OtpPurpose.TwoFactor)]
    [InlineData(OtpPurpose.PhoneChange)]
    public async Task Null_phone_skips_whatsapp(OtpPurpose purpose)
    {
        var email = new RecordingEmailSender();
        var whatsApp = new RecordingWhatsAppSender();
        await Build(email, whatsApp).SendAsync(
            new OtpRecipient("user@example.com", null, null),
            purpose, "123456", 15, default);

        Assert.Equal(1, email.Calls);
        Assert.Equal(0, whatsApp.Calls);
    }

    [Fact]
    public async Task PhoneChange_calls_email()
    {
        var email = new RecordingEmailSender();
        var whatsApp = new RecordingWhatsAppSender();
        await Build(email, whatsApp).SendAsync(
            new OtpRecipient("user@example.com", null, "+27820000002"),
            OtpPurpose.PhoneChange, "123456", 15, default);

        Assert.Equal(1, email.Calls);
        Assert.Equal(1, whatsApp.Calls);
    }

    [Fact]
    public async Task PhoneChange_sends_email_and_whatsapp_to_distinct_destinations()
    {
        var email = new RecordingEmailSender();
        var whatsApp = new RecordingWhatsAppSender();
        await Build(email, whatsApp).SendAsync(
            new OtpRecipient("account@example.com", null, "+27820000002"),
            OtpPurpose.PhoneChange, "123456", 15, default);

        Assert.Equal("account@example.com", email.LastEmail);
        Assert.Equal("+27820000002", whatsApp.LastPhone);
    }

    [Fact]
    public async Task Throwing_email_propagates()
    {
        var email = new RecordingEmailSender(throws: true);
        var whatsApp = new RecordingWhatsAppSender();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Build(email, whatsApp).SendAsync(
                new OtpRecipient("user@example.com", null, "+27821234567"),
                OtpPurpose.EmailConfirmation, "123456", 15, default));
    }
}
