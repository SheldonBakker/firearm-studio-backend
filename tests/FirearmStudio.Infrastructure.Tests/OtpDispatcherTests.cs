using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Services;
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

    private static OtpDispatcher Build(RecordingEmailSender email) => new(email);

    [Theory]
    [InlineData(OtpPurpose.EmailConfirmation)]
    [InlineData(OtpPurpose.PasswordReset)]
    [InlineData(OtpPurpose.Invite)]
    [InlineData(OtpPurpose.TwoFactor)]
    public async Task Every_purpose_sends_exactly_one_email(OtpPurpose purpose)
    {
        var email = new RecordingEmailSender();
        await Build(email).SendAsync(
            new OtpRecipient("user@example.com", null),
            purpose, "123456", 15, default);

        Assert.Equal(1, email.Calls);
        Assert.Equal("user@example.com", email.LastEmail);
    }

    [Fact]
    public async Task Throwing_email_propagates()
    {
        var email = new RecordingEmailSender(throws: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Build(email).SendAsync(
                new OtpRecipient("user@example.com", null),
                OtpPurpose.EmailConfirmation, "123456", 15, default));
    }
}
