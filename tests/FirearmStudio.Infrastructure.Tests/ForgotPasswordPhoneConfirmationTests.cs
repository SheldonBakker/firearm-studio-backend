using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Auth;
using FirearmStudio.Application.Auth.ForgotPassword;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class ForgotPasswordPhoneConfirmationTests
{
    private sealed class FakeAccounts(UserAccount? account) : IUserAccountService
    {
        public Task<UserAccount?> FindByEmailAsync(string email, CancellationToken ct) => Task.FromResult(account);
        public Task<(UserAccount? Account, IReadOnlyList<string> Errors)> CreateAsync(string email, string password, CancellationToken ct) => throw new NotSupportedException();
        public Task<PasswordCheckResult> CheckPasswordAsync(Guid userId, string password, CancellationToken ct) => throw new NotSupportedException();
        public Task ConfirmEmailAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> SetPasswordAsync(Guid userId, string newPassword, CancellationToken ct) => throw new NotSupportedException();
        public Task SetTwoFactorEnabledAsync(Guid userId, bool enabled, CancellationToken ct) => Task.CompletedTask;
        public Task SetPhoneNumberAsync(Guid userId, string? phoneE164, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeOtp : IOtpService
    {
        public Task<OtpIssueResult> IssueAsync(Guid userId, OtpPurpose purpose, CancellationToken ct) =>
            Task.FromResult(new OtpIssueResult(OtpIssueStatus.Issued, "123456", null));
        public Task<OtpVerifyResult> VerifyAsync(Guid userId, OtpPurpose purpose, string code, CancellationToken ct) => throw new NotSupportedException();
        public Task InvalidateAsync(Guid userId, OtpPurpose purpose, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class RecordingDispatcher : IOtpDispatcher
    {
        public OtpRecipient? Recipient { get; private set; }
        public int Calls { get; private set; }
        public Task SendAsync(OtpRecipient recipient, OtpPurpose purpose, string code, int expiresInMinutes, CancellationToken ct)
        {
            Calls++;
            Recipient = recipient;
            return Task.CompletedTask;
        }
    }

    private static UserAccount Account() =>
        new(Guid.NewGuid(), "user@example.com", true, false, "+27820000001");

    [Fact]
    public async Task Known_account_receives_the_password_reset_code_by_email()
    {
        var dispatcher = new RecordingDispatcher();
        var handler = new ForgotPasswordCommandHandler(
            new FakeAccounts(Account()), new FakeOtp(), dispatcher);

        var result = await handler.Handle(
            new ForgotPasswordCommand(new ForgotPasswordRequest("user@example.com")), default);

        Assert.False(result.IsError);
        Assert.Equal(1, dispatcher.Calls);
        Assert.Equal("user@example.com", dispatcher.Recipient!.Email);
    }
}
