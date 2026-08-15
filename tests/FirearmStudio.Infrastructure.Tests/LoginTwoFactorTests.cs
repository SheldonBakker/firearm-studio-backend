using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Auth;
using FirearmStudio.Application.Auth.Login;
using FirearmStudio.Application.Auth.TwoFactor;
using FirearmStudio.Domain.Authentication;
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
        public (Guid UserId, bool Enabled)? TwoFactorCall { get; private set; }

        public Task SetTwoFactorEnabledAsync(Guid userId, bool enabled, CancellationToken ct)
        {
            TwoFactorCall = (userId, enabled);
            return Task.CompletedTask;
        }
        public Task SetPhoneNumberAsync(Guid userId, string? phoneE164, bool confirmed, CancellationToken ct) => Task.CompletedTask;
        public Task SetPendingPhoneNumberAsync(Guid userId, string phoneE164, CancellationToken ct) => Task.CompletedTask;
        public Task ClearPendingPhoneNumberAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
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
        public bool PreAuthIssued { get; private set; }

        public string IssuePreAuthToken(Guid userId, string email)
        {
            PreAuthIssued = true;
            return PreAuthToken;
        }
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

        public Task InvalidateAsync(Guid userId, OtpPurpose purpose, CancellationToken ct) => Task.CompletedTask;
    }

    internal sealed class FakeCurrentUser(Guid userId) : ICurrentUserService
    {
        public CurrentUser User { get; } = new() { Id = userId, IsAuthenticated = true };
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
        new(Guid.NewGuid(), "user@example.com", emailConfirmed, twoFactor, "+27820000001", true, null);

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
    public async Task Throttled_two_factor_returns_an_error_and_no_challenge()
    {
        var accounts = new FakeAccounts { Account = Account(twoFactor: true) };
        var tokens = new FakeTokens { PreAuthToken = "the-pre-auth" };
        var otp = new FakeOtp { IssueStatus = OtpIssueStatus.Throttled };
        var dispatcher = new RecordingDispatcher();

        var result = await Build(accounts, tokens, otp, dispatcher).Handle(
            new LoginCommand(new LoginRequest("user@example.com", "pw")), default);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrorCodes.ChallengeUnavailable, result.FirstError.Code);
        Assert.False(tokens.PreAuthIssued);
        Assert.Equal(0, dispatcher.Calls);
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

    private static LoginVerifyCommandHandler BuildVerify(FakeTokens tokens, FakeOtp otp) => new(tokens, otp);

    [Fact]
    public async Task Verify_with_invalid_pre_auth_is_unauthorized()
    {
        var tokens = new FakeTokens { PreAuth = null };
        var otp = new FakeOtp();

        var result = await BuildVerify(tokens, otp).Handle(
            new LoginVerifyCommand(new LoginVerifyRequest("bad", "123456")), default);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrorCodes.PreAuthInvalid, result.FirstError.Code);
        Assert.False(otp.VerifyCalled);
    }

    [Fact]
    public async Task Verify_with_wrong_code_is_a_validation_error()
    {
        var tokens = new FakeTokens { PreAuth = new PreAuthPrincipal(Guid.NewGuid(), "user@example.com") };
        var otp = new FakeOtp { VerifyResult = OtpVerifyResult.Invalid };

        var result = await BuildVerify(tokens, otp).Handle(
            new LoginVerifyCommand(new LoginVerifyRequest("ok", "000000")), default);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrorCodes.CodeInvalid, result.FirstError.Code);
    }

    [Fact]
    public async Task Verify_with_correct_code_returns_tokens()
    {
        var userId = Guid.NewGuid();
        var tokens = new FakeTokens { PreAuth = new PreAuthPrincipal(userId, "user@example.com") };
        var otp = new FakeOtp { VerifyResult = OtpVerifyResult.Valid };

        var result = await BuildVerify(tokens, otp).Handle(
            new LoginVerifyCommand(new LoginVerifyRequest("ok", "123456")), default);

        Assert.False(result.IsError);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal((userId, "user@example.com"), tokens.IssuedFor);
    }

    [Fact]
    public async Task Verify_cannot_replay_a_previously_consumed_code()
    {
        var userId = Guid.NewGuid();
        var tokens = new FakeTokens { PreAuth = new PreAuthPrincipal(userId, "user@example.com") };
        var otp = new FakeOtp { VerifyResult = OtpVerifyResult.Valid };

        var first = await BuildVerify(tokens, otp).Handle(
            new LoginVerifyCommand(new LoginVerifyRequest("ok", "123456")), default);

        Assert.False(first.IsError);

        // Mirrors OtpService.VerifyAsync: a consumed code is gone on replay, not merely wrong.
        otp.VerifyResult = OtpVerifyResult.NotFound;

        var second = await BuildVerify(tokens, otp).Handle(
            new LoginVerifyCommand(new LoginVerifyRequest("ok", "123456")), default);

        Assert.True(second.IsError);
        Assert.Equal(AuthErrorCodes.CodeInvalid, second.FirstError.Code);
    }

    [Fact]
    public async Task Enable_two_factor_needs_no_password()
    {
        var accounts = new FakeAccounts();
        var userId = Guid.NewGuid();
        var handler = new EnableTwoFactorCommandHandler(new FakeCurrentUser(userId), accounts);

        var result = await handler.Handle(new EnableTwoFactorCommand(), default);

        Assert.False(result.IsError);
        Assert.Equal((userId, true), accounts.TwoFactorCall);
    }

    [Fact]
    public async Task Disable_two_factor_with_the_right_password_flips_the_flag_off()
    {
        var accounts = new FakeAccounts { PasswordResult = PasswordCheckResult.Succeeded };
        var userId = Guid.NewGuid();
        var handler = new DisableTwoFactorCommandHandler(new FakeCurrentUser(userId), accounts);

        var result = await handler.Handle(
            new DisableTwoFactorCommand(new DisableTwoFactorRequest("CorrectHorse123")), default);

        Assert.False(result.IsError);
        Assert.Equal((userId, false), accounts.TwoFactorCall);
    }

    [Fact]
    public async Task Disable_two_factor_with_a_wrong_password_changes_nothing()
    {
        var accounts = new FakeAccounts { PasswordResult = PasswordCheckResult.Failed };
        var handler = new DisableTwoFactorCommandHandler(new FakeCurrentUser(Guid.NewGuid()), accounts);

        var result = await handler.Handle(
            new DisableTwoFactorCommand(new DisableTwoFactorRequest("wrong")), default);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrorCodes.InvalidCredentials, result.FirstError.Code);
        Assert.Null(accounts.TwoFactorCall);
    }

    [Fact]
    public async Task Disable_two_factor_while_locked_out_changes_nothing()
    {
        var accounts = new FakeAccounts { PasswordResult = PasswordCheckResult.LockedOut };
        var handler = new DisableTwoFactorCommandHandler(new FakeCurrentUser(Guid.NewGuid()), accounts);

        var result = await handler.Handle(
            new DisableTwoFactorCommand(new DisableTwoFactorRequest("CorrectHorse123")), default);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrorCodes.LockedOut, result.FirstError.Code);
        Assert.Null(accounts.TwoFactorCall);
    }
}
