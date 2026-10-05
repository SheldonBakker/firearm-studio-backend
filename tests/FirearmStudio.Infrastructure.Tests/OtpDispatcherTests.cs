using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Email;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Services;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class OtpDispatcherTests
{
    private sealed class RecordingEmailSender(bool throws = false) : ITransactionalEmailSender
    {
        public int Calls { get; private set; }
        public OtpEmail? LastOtp { get; private set; }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Calls++;
            if (message is OtpEmail otp)
            {
                LastOtp = otp;
            }

            if (throws)
            {
                throw new InvalidOperationException("email down");
            }

            return Task.CompletedTask;
        }
    }

    private static OtpDispatcher Build(RecordingEmailSender sender) => new(sender);

    [Theory]
    [InlineData(OtpPurpose.EmailConfirmation)]
    [InlineData(OtpPurpose.PasswordReset)]
    [InlineData(OtpPurpose.Invite)]
    [InlineData(OtpPurpose.TwoFactor)]
    public async Task Every_purpose_sends_exactly_one_email(OtpPurpose purpose)
    {
        var sender = new RecordingEmailSender();
        await Build(sender).SendAsync(
            new OtpRecipient("user@example.com", null),
            purpose, "123456", 15, default);

        Assert.Equal(1, sender.Calls);
        Assert.Equal("user@example.com", sender.LastOtp!.Email);
    }

    [Fact]
    public async Task Recipient_name_and_code_are_forwarded()
    {
        var sender = new RecordingEmailSender();
        await Build(sender).SendAsync(
            new OtpRecipient("user@example.com", "Alice"),
            OtpPurpose.EmailConfirmation, "654321", 30, default);

        Assert.Equal("Alice", sender.LastOtp!.Name);
        Assert.Equal("654321", sender.LastOtp.Code);
        Assert.Equal(30, sender.LastOtp.ExpiresInMinutes);
    }

    [Fact]
    public async Task Throwing_sender_propagates()
    {
        var sender = new RecordingEmailSender(throws: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Build(sender).SendAsync(
                new OtpRecipient("user@example.com", null),
                OtpPurpose.EmailConfirmation, "123456", 15, default));
    }
}
