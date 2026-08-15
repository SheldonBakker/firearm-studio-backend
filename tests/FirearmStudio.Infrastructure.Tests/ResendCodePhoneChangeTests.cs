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
        public int LookupCalls { get; private set; }

        public Task<UserAccount?> FindByEmailAsync(string email, CancellationToken ct)
        {
            LookupCalls++;
            return Task.FromResult(account);
        }

        public Task<(UserAccount? Account, IReadOnlyList<string> Errors)> CreateAsync(string email, string password, CancellationToken ct) => throw new NotSupportedException();
        public Task<PasswordCheckResult> CheckPasswordAsync(Guid userId, string password, CancellationToken ct) => throw new NotSupportedException();
        public Task ConfirmEmailAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> SetPasswordAsync(Guid userId, string newPassword, CancellationToken ct) => throw new NotSupportedException();
        public Task SetTwoFactorEnabledAsync(Guid userId, bool enabled, CancellationToken ct) => Task.CompletedTask;
        public Task SetPhoneNumberAsync(Guid userId, string? phoneE164, bool confirmed, CancellationToken ct) => Task.CompletedTask;
        public Task SetPendingPhoneNumberAsync(Guid userId, string phoneE164, CancellationToken ct) => Task.CompletedTask;
        public Task ClearPendingPhoneNumberAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
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
        public Task InvalidateAsync(Guid userId, OtpPurpose purpose, CancellationToken ct) => Task.CompletedTask;
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
        new(Guid.NewGuid(), "user@example.com", true, false, "+27820000001", true, pending);

    [Theory]
    [InlineData("PhoneChange")]
    [InlineData("TwoFactor")]
    public async Task Non_resendable_purposes_are_refused_without_issuing_or_dispatching(string purpose)
    {
        var otp = new FakeOtp();
        var dispatcher = new RecordingDispatcher();
        var accounts = new FakeAccounts(Account("+27820000002"));
        var handler = new ResendCodeCommandHandler(accounts, otp, dispatcher);

        var result = await handler.Handle(
            new ResendCodeCommand(new ResendCodeRequest("user@example.com", purpose)), default);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrorCodes.PurposeNotResendable, result.FirstError.Code);
        Assert.Equal(0, otp.IssueCalls);
        Assert.Equal(0, dispatcher.Calls);
        Assert.Null(dispatcher.Recipient);

        Assert.Equal(0, accounts.LookupCalls);
    }

    [Fact]
    public async Task PhoneChange_resend_with_no_pending_is_refused_the_same_way()
    {
        var otp = new FakeOtp();
        var dispatcher = new RecordingDispatcher();
        var handler = new ResendCodeCommandHandler(new FakeAccounts(Account(null)), otp, dispatcher);

        var result = await handler.Handle(
            new ResendCodeCommand(new ResendCodeRequest("user@example.com", "PhoneChange")), default);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrorCodes.PurposeNotResendable, result.FirstError.Code);
        Assert.Equal(0, otp.IssueCalls);
        Assert.Equal(0, dispatcher.Calls);
    }

    [Theory]
    [InlineData("PhoneChange")]
    [InlineData("TwoFactor")]
    public async Task Refusal_is_identical_for_an_unknown_address(string purpose)
    {
        var known = new ResendCodeCommandHandler(
            new FakeAccounts(Account("+27820000002")), new FakeOtp(), new RecordingDispatcher());
        var unknown = new ResendCodeCommandHandler(
            new FakeAccounts(null), new FakeOtp(), new RecordingDispatcher());

        var forKnown = await known.Handle(
            new ResendCodeCommand(new ResendCodeRequest("user@example.com", purpose)), default);
        var forUnknown = await unknown.Handle(
            new ResendCodeCommand(new ResendCodeRequest("nobody@example.com", purpose)), default);

        Assert.True(forKnown.IsError);
        Assert.True(forUnknown.IsError);
        Assert.Equal(forKnown.FirstError.Code, forUnknown.FirstError.Code);
        Assert.Equal(forKnown.FirstError.Description, forUnknown.FirstError.Description);
        Assert.Equal(forKnown.FirstError.Type, forUnknown.FirstError.Type);
    }

    [Theory]
    [InlineData("EmailConfirmation")]
    [InlineData("PasswordReset")]
    [InlineData("Invite")]
    public async Task Resendable_purposes_still_target_the_confirmed_number(string purpose)
    {
        var dispatcher = new RecordingDispatcher();
        var handler = new ResendCodeCommandHandler(new FakeAccounts(Account("+27820000002")), new FakeOtp(), dispatcher);

        var result = await handler.Handle(
            new ResendCodeCommand(new ResendCodeRequest("user@example.com", purpose)), default);

        Assert.False(result.IsError);
        Assert.Equal("+27820000001", dispatcher.Recipient!.PhoneNumber);
    }
}
