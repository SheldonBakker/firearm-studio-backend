using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Auth;
using FirearmStudio.Application.Auth.Login;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class LoginTwoFactorTests
{
    internal sealed class FakeAccounts : IUserAccountService
    {
        public UserAccount? Account { get; set; }
        public PasswordCheckResult PasswordResult { get; set; } = PasswordCheckResult.Succeeded;

        public Task<UserAccount?> FindByEmailAsync(string email, CancellationToken ct) => Task.FromResult(Account);
        public Task<(UserAccount? Account, IReadOnlyList<string> Errors)> CreateAsync(string email, string password, CancellationToken ct) => throw new NotSupportedException();
        public Task<PasswordCheckResult> CheckPasswordAsync(Guid userId, string password, CancellationToken ct) => Task.FromResult(PasswordResult);
        public Task ConfirmEmailAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> SetPasswordAsync(Guid userId, string newPassword, CancellationToken ct) => throw new NotSupportedException();
        public Task SetTwoFactorEnabledAsync(Guid userId, bool enabled, CancellationToken ct) => Task.CompletedTask;
        public Task SetPhoneNumberAsync(Guid userId, string? phoneE164, bool confirmed, CancellationToken ct) => Task.CompletedTask;
        public Task SetPendingPhoneNumberAsync(Guid userId, string phoneE164, CancellationToken ct) => Task.CompletedTask;
        public Task<string?> ConfirmPhoneChangeAsync(Guid userId, CancellationToken ct) => Task.FromResult<string?>(null);
    }

    internal sealed class FakeTokens : ITokenService
    {
        public PreAuthPrincipal? PreAuth { get; set; }
        public string PreAuthToken { get; set; } = "pre-auth-token";
        public TokenPair Pair { get; set; } = new("access", "refresh", DateTime.UtcNow.AddMinutes(15));
        public (Guid UserId, string Email)? IssuedFor { get; private set; }

        public Task<TokenPair> IssueAsync(Guid userId, string email, CancellationToken ct)
        {
            IssuedFor = (userId, email);
            return Task.FromResult(Pair);
        }
        public Task<(TokenPair? Pair, RefreshFailure? Failure)> RefreshAsync(string refreshToken, CancellationToken ct) => throw new NotSupportedException();
        public Task RevokeAsync(string refreshToken, CancellationToken ct) => Task.CompletedTask;
        public Task RevokeAllAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
        public string IssuePreAuthToken(Guid userId, string email) => PreAuthToken;
        public PreAuthPrincipal? ValidatePreAuthToken(string token) => PreAuth;
    }

    internal sealed class FakeOtp : IOtpService
    {
        public OtpIssueStatus IssueStatus { get; set; } = OtpIssueStatus.Issued;
        public OtpVerifyResult VerifyResult { get; set; } = OtpVerifyResult.Valid;
        public int IssueCalls { get; private set; }
        public bool VerifyCalled { get; private set; }

        public Task<OtpIssueResult> IssueAsync(Guid userId, OtpPurpose purpose, CancellationToken ct)
        {
            IssueCalls++;
            return Task.FromResult(new OtpIssueResult(IssueStatus, IssueStatus == OtpIssueStatus.Issued ? "123456" : null, null));
        }
        public Task<OtpVerifyResult> VerifyAsync(Guid userId, OtpPurpose purpose, string code, CancellationToken ct)
        {
            VerifyCalled = true;
            return Task.FromResult(VerifyResult);
        }
    }

    internal sealed class RecordingDispatcher : IOtpDispatcher
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

    private static UserAccount Account(bool twoFactor, bool emailConfirmed = true) =>
        new(Guid.NewGuid(), "user@example.com", emailConfirmed, twoFactor, "+27820000001", null);

    private static LoginCommandHandler Build(FakeAccounts accounts, FakeTokens tokens, FakeOtp otp, RecordingDispatcher dispatcher) =>
        new(accounts, tokens, otp, dispatcher);

    [Fact]
    public async Task Two_factor_off_returns_tokens_arm()
    {
        var accounts = new FakeAccounts { Account = Account(twoFactor: false) };
        var otp = new FakeOtp();
        var dispatcher = new RecordingDispatcher();

        var result = await Build(accounts, new FakeTokens(), otp, dispatcher).Handle(
            new LoginCommand(new LoginRequest("user@example.com", "pw")), default);

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.Tokens);
        Assert.Null(result.Value.Challenge);
        Assert.Equal(0, otp.IssueCalls);
        Assert.Equal(0, dispatcher.Calls);
    }

    [Fact]
    public async Task Two_factor_on_returns_challenge_and_no_tokens()
    {
        var accounts = new FakeAccounts { Account = Account(twoFactor: true) };
        var tokens = new FakeTokens { PreAuthToken = "the-pre-auth" };
        var otp = new FakeOtp();
        var dispatcher = new RecordingDispatcher();

        var result = await Build(accounts, tokens, otp, dispatcher).Handle(
            new LoginCommand(new LoginRequest("user@example.com", "pw")), default);

        Assert.False(result.IsError);
        Assert.Null(result.Value.Tokens);
        Assert.NotNull(result.Value.Challenge);
        Assert.True(result.Value.Challenge!.RequiresTwoFactor);
        Assert.Equal("the-pre-auth", result.Value.Challenge.PreAuthToken);
        Assert.Equal(1, otp.IssueCalls);
        Assert.Equal(1, dispatcher.Calls);
        Assert.Equal(OtpPurpose.TwoFactor, dispatcher.Purpose);
        Assert.Equal("+27820000001", dispatcher.Recipient!.PhoneNumber);
    }

    [Fact]
    public async Task Wrong_password_is_an_error_with_no_challenge()
    {
        var accounts = new FakeAccounts { Account = Account(twoFactor: true), PasswordResult = PasswordCheckResult.Failed };
        var otp = new FakeOtp();
        var dispatcher = new RecordingDispatcher();

        var result = await Build(accounts, new FakeTokens(), otp, dispatcher).Handle(
            new LoginCommand(new LoginRequest("user@example.com", "wrong")), default);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrorCodes.InvalidCredentials, result.FirstError.Code);
        Assert.Equal(0, dispatcher.Calls);
    }

    [Fact]
    public async Task Unconfirmed_email_still_blocks_login_even_with_two_factor_enabled()
    {
        var accounts = new FakeAccounts { Account = Account(twoFactor: true, emailConfirmed: false) };
        var otp = new FakeOtp();
        var dispatcher = new RecordingDispatcher();

        var result = await Build(accounts, new FakeTokens(), otp, dispatcher).Handle(
            new LoginCommand(new LoginRequest("user@example.com", "pw")), default);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrorCodes.EmailNotConfirmed, result.FirstError.Code);
        Assert.Equal(0, otp.IssueCalls);
        Assert.Equal(0, dispatcher.Calls);
    }

    [Fact]
    public async Task Locked_out_is_forbidden()
    {
        var accounts = new FakeAccounts { Account = Account(twoFactor: true), PasswordResult = PasswordCheckResult.LockedOut };

        var result = await Build(accounts, new FakeTokens(), new FakeOtp(), new RecordingDispatcher()).Handle(
            new LoginCommand(new LoginRequest("user@example.com", "pw")), default);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrorCodes.LockedOut, result.FirstError.Code);
    }
}
