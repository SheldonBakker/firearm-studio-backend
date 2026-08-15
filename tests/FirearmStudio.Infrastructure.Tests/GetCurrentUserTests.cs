using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Me.GetCurrentUser;
using FirearmStudio.Domain.Authentication;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class GetCurrentUserTests
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
        public Task ClearPendingPhoneNumberAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
        public Task<string?> ConfirmPhoneChangeAsync(Guid userId, CancellationToken ct) => Task.FromResult<string?>(null);
    }

    private sealed class FakeCurrentUser(CurrentUser user) : ICurrentUserService
    {
        public CurrentUser User { get; } = user;
    }

    private static CurrentUser Principal(Guid id, string? email, params string[] roles) => new()
    {
        Id = id,
        Email = email,
        Roles = roles,
        IsAuthenticated = true,
    };

    [Fact]
    public async Task Two_factor_enabled_with_confirmed_phone_and_no_pending_change_reports_all_fields()
    {
        var userId = Guid.NewGuid();
        var account = new UserAccount(userId, "user@example.com", true, true, "+27820000001", true, null);
        var handler = new GetCurrentUserQueryHandler(
            new FakeCurrentUser(Principal(userId, "user@example.com", "Admin")),
            new FakeAccounts(account));

        var result = await handler.Handle(new GetCurrentUserQuery(), default);

        Assert.False(result.IsError);
        Assert.True(result.Value.TwoFactorEnabled);
        Assert.Equal("+27820000001", result.Value.PhoneNumber);
        Assert.True(result.Value.PhoneNumberConfirmed);
        Assert.Null(result.Value.PendingPhoneNumber);
    }

    [Fact]
    public async Task Mid_phone_change_reports_the_pending_number_alongside_the_still_current_confirmed_number()
    {
        var userId = Guid.NewGuid();
        var account = new UserAccount(userId, "user@example.com", true, false, "+27820000001", true, "+27820000002");
        var handler = new GetCurrentUserQueryHandler(
            new FakeCurrentUser(Principal(userId, "user@example.com")),
            new FakeAccounts(account));

        var result = await handler.Handle(new GetCurrentUserQuery(), default);

        Assert.False(result.IsError);
        Assert.Equal("+27820000001", result.Value.PhoneNumber);
        Assert.True(result.Value.PhoneNumberConfirmed);
        Assert.Equal("+27820000002", result.Value.PendingPhoneNumber);
    }

    [Fact]
    public async Task No_phone_on_file_reports_nulls_and_false_not_empty_strings()
    {
        var userId = Guid.NewGuid();
        var account = new UserAccount(userId, "user@example.com", true, false, null, false, null);
        var handler = new GetCurrentUserQueryHandler(
            new FakeCurrentUser(Principal(userId, "user@example.com")),
            new FakeAccounts(account));

        var result = await handler.Handle(new GetCurrentUserQuery(), default);

        Assert.False(result.IsError);
        Assert.False(result.Value.TwoFactorEnabled);
        Assert.Null(result.Value.PhoneNumber);
        Assert.False(result.Value.PhoneNumberConfirmed);
        Assert.Null(result.Value.PendingPhoneNumber);
    }

    [Fact]
    public async Task Existing_consumers_still_see_id_email_and_roles_unchanged()
    {
        var userId = Guid.NewGuid();
        var account = new UserAccount(userId, "user@example.com", true, true, "+27820000001", true, null);
        var handler = new GetCurrentUserQueryHandler(
            new FakeCurrentUser(Principal(userId, "user@example.com", "Admin", "Owner")),
            new FakeAccounts(account));

        var result = await handler.Handle(new GetCurrentUserQuery(), default);

        Assert.False(result.IsError);
        Assert.Equal(userId, result.Value.Id);
        Assert.Equal("user@example.com", result.Value.Email);
        Assert.Equal(["Admin", "Owner"], result.Value.Roles);
    }
}
