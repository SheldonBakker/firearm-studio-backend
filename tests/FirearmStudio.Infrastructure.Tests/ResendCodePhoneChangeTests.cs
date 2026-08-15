using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Auth;
using FirearmStudio.Application.Auth.ResendCode;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class ResendCodePhoneChangeTests
{
    private sealed class FakeAccounts(UserAccount? account) : IUserAccountService
    {
        public Task<UserAccount?> FindByEmailAsync(string email, CancellationToken ct) => Task.FromResult(account);
        public Task<(UserAccount? Account, IReadOnlyList<string> Errors)> CreateAsync(string email, string password, CancellationToken ct) => throw new NotSupportedException();
        public Task<PasswordCheckResult> CheckPasswordAsync(Guid userId, string password, CancellationToken ct) => throw new NotSupportedException();
        public Task ConfirmEmailAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> SetPasswordAsync(Guid userId, string newPassword, CancellationToken ct) => throw new NotSupportedException();
        public Task SetTwoFactorEnabledAsync(Guid userId, bool enabled, CancellationToken ct) => Task.CompletedTask;
        public Task SetPhoneNumberAsync(Guid userId, string? phoneE164, bool confirmed, CancellationToken ct) => Task.CompletedTask;
        public Task SetPendingPhoneNumberAsync(Guid userId, string phoneE164, CancellationToken ct) => Task.CompletedTask;
        public Task<string?> ConfirmPhoneChangeAsync(Guid userId, CancellationToken ct) => Task.FromResult<string?>(null);
    }

    private sealed class FakeOtp : IOtpService
    {
        public int IssueCalls { get; private set; }
        public Task<OtpIssueResult> IssueAsync(Guid userId, OtpPurpose purpose, CancellationToken ct)
        {
            IssueCalls++;
            return Task.FromResult(new OtpIssueResult(OtpIssueStatus.Issued, "123456", null));
        }
        public Task<OtpVerifyResult> VerifyAsync(Guid userId, OtpPurpose purpose, string code, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class RecordingDispatcher : IOtpDispatcher
    {
        public OtpRecipient? Recipient { get; private set; }
        public OtpPurpose? Purpose { get; private set; }
        public int Calls { get; private set; }
        public Task SendAsync(OtpRecipient recipient, OtpPurpose purpose, string code, int expiresInMinutes, CancellationToken ct)
        {
            Calls++;
            Recipient = recipient;
            Purpose = purpose;
            return Task.CompletedTask;
        }
    }

    private static UserAccount Account(string? pending) =>
        new(Guid.NewGuid(), "user@example.com", true, false, "+27820000001", pending);

    [Fact]
    public async Task PhoneChange_resend_targets_the_pending_number()
    {
        var otp = new FakeOtp();
        var dispatcher = new RecordingDispatcher();
        var handler = new ResendCodeCommandHandler(new FakeAccounts(Account("+27820000002")), otp, dispatcher);

        var result = await handler.Handle(
            new ResendCodeCommand(new ResendCodeRequest("user@example.com", "PhoneChange")), default);

        Assert.False(result.IsError);
        Assert.Equal("+27820000002", dispatcher.Recipient!.PhoneNumber);
        Assert.Equal(OtpPurpose.PhoneChange, dispatcher.Purpose);
    }

    [Fact]
    public async Task PhoneChange_resend_with_no_pending_returns_phone_missing()
    {
        var otp = new FakeOtp();
        var dispatcher = new RecordingDispatcher();
        var handler = new ResendCodeCommandHandler(new FakeAccounts(Account(null)), otp, dispatcher);

        var result = await handler.Handle(
            new ResendCodeCommand(new ResendCodeRequest("user@example.com", "PhoneChange")), default);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrorCodes.PhoneMissing, result.FirstError.Code);
        Assert.Equal(0, otp.IssueCalls);
        Assert.Equal(0, dispatcher.Calls);
    }

    [Theory]
    [InlineData("EmailConfirmation")]
    [InlineData("PasswordReset")]
    [InlineData("Invite")]
    [InlineData("TwoFactor")]
    public async Task Non_phone_change_resend_targets_the_confirmed_number(string purpose)
    {
        var dispatcher = new RecordingDispatcher();
        var handler = new ResendCodeCommandHandler(new FakeAccounts(Account("+27820000002")), new FakeOtp(), dispatcher);

        var result = await handler.Handle(
            new ResendCodeCommand(new ResendCodeRequest("user@example.com", purpose)), default);

        Assert.False(result.IsError);
        Assert.Equal("+27820000001", dispatcher.Recipient!.PhoneNumber);
    }
}
