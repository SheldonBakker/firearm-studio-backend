using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Users;
using FirearmStudio.Application.Users.UpdatePhone;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Identity;
using FirearmStudio.Infrastructure.Persistence;
using FirearmStudio.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class PhoneChangeTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private sealed class FixedCurrentUser(Guid id, string email) : ICurrentUserService
    {
        public CurrentUser User { get; } = new() { Id = id, Email = email, IsAuthenticated = true };
    }

    private sealed class CapturingDispatcher : IOtpDispatcher
    {
        public string? LastCode { get; private set; }
        public OtpPurpose? LastPurpose { get; private set; }
        public OtpRecipient? LastRecipient { get; private set; }

        public Task SendAsync(OtpRecipient recipient, OtpPurpose purpose, string code, int expiresInMinutes, CancellationToken ct)
        {
            LastRecipient = recipient;
            LastPurpose = purpose;
            LastCode = code;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingWhatsAppSender : IWhatsAppSender
    {
        public Task SendOtpAsync(string phoneE164, OtpPurpose purpose, string code, int expiresInMinutes, CancellationToken ct) =>
            throw new HttpRequestException("waha down");
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task SendOtpAsync(string email, string? name, OtpPurpose purpose, string code, int expiresInMinutes, CancellationToken ct) =>
            throw new InvalidOperationException("email must not be used for a phone change");
    }

    private static UpdatePhoneCommandHandler BuildUpdateHandler(
        ICurrentUserService currentUser,
        IUserAccountService accounts,
        IOtpService otp,
        IOtpDispatcher dispatcher) =>
        new(currentUser, accounts, otp, dispatcher, NullLogger<UpdatePhoneCommandHandler>.Instance);

    private static UserManager<AppIdentityUser> BuildUserManager(AuthDbContext auth)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(auth);
        services.AddIdentityCore<AppIdentityUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireNonAlphanumeric = false;
        }).AddEntityFrameworkStores<AuthDbContext>();
        return services.BuildServiceProvider().GetRequiredService<UserManager<AppIdentityUser>>();
    }

    private async Task<(IdentityUserAccountService Accounts, OtpService Otp, ApplicationDbContext App,
        BypassTenantContext Tenant, FixedCurrentUser CurrentUser, Guid UserId, Guid CompanyId,
        TestTimeProvider Clock)> SeedAsync()
    {
        await fixture.MigrateAllAsync();

        var clock = new TestTimeProvider(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var auth = fixture.CreateAuthDbContext();
        var tenant = new BypassTenantContext();
        var app = fixture.CreateDbContext(tenant);

        var userId = Guid.NewGuid();
        var email = $"{Guid.NewGuid():N}@example.com";
        var companyId = Guid.NewGuid();

        auth.Users.Add(new AppIdentityUser
        {
            Id = userId,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
        });
        await auth.SaveChangesAsync();

        tenant.CompanyId = companyId;
        app.Companies.Add(new Company { Id = companyId, Name = "Co" });
        app.AppUsers.Add(new AppUser { CompanyId = companyId, AuthUserId = userId, Email = email, IsActive = true });
        await app.SaveChangesAsync();

        var accounts = new IdentityUserAccountService(BuildUserManager(auth));
        var otp = new OtpService(auth, new PasswordHasher<AppIdentityUser>(), clock);

        return (accounts, otp, app, tenant, new FixedCurrentUser(userId, email), userId, companyId, clock);
    }

    [Fact]
    public async Task Verify_promotes_pending_and_mirrors_to_app_user()
    {
        var (accounts, otp, app, tenant, currentUser, userId, companyId, _) = await SeedAsync();
        var dispatcher = new CapturingDispatcher();

        var request = BuildUpdateHandler(currentUser, accounts, otp, dispatcher);
        var requested = await request.Handle(new UpdatePhoneCommand(new UpdatePhoneRequest("+27821234567")), default);

        Assert.False(requested.IsError);
        Assert.Equal(OtpPurpose.PhoneChange, dispatcher.LastPurpose);
        Assert.Equal("+27821234567", dispatcher.LastRecipient!.PhoneNumber);
        Assert.Equal(currentUser.User.Email, dispatcher.LastRecipient.Email);

        var verify = new VerifyPhoneCommandHandler(currentUser, accounts, otp, app, tenant);
        var result = await verify.Handle(new VerifyPhoneCommand(new VerifyPhoneRequest(dispatcher.LastCode!)), default);

        Assert.False(result.IsError);

        await using var authAfter = fixture.CreateAuthDbContext();
        var identityUser = await authAfter.Users.SingleAsync(u => u.Id == userId);
        Assert.Equal("+27821234567", identityUser.PhoneNumber);
        Assert.True(identityUser.PhoneNumberConfirmed);
        Assert.Null(identityUser.PendingPhoneNumber);

        await using var appAfter = fixture.CreateDbContext(companyId);
        var appUser = await appAfter.AppUsers.SingleAsync(u => u.AuthUserId == userId);
        Assert.Equal("+27821234567", appUser.PhoneNumber);
    }

    [Fact]
    public async Task Wrong_code_does_not_promote()
    {
        var (accounts, otp, app, tenant, currentUser, userId, _, _) = await SeedAsync();
        var dispatcher = new CapturingDispatcher();

        var request = BuildUpdateHandler(currentUser, accounts, otp, dispatcher);
        await request.Handle(new UpdatePhoneCommand(new UpdatePhoneRequest("+27821234567")), default);

        var wrong = dispatcher.LastCode == "000000" ? "111111" : "000000";

        var verify = new VerifyPhoneCommandHandler(currentUser, accounts, otp, app, tenant);
        var result = await verify.Handle(new VerifyPhoneCommand(new VerifyPhoneRequest(wrong)), default);

        Assert.True(result.IsError);

        await using var authAfter = fixture.CreateAuthDbContext();
        var identityUser = await authAfter.Users.SingleAsync(u => u.Id == userId);
        Assert.Null(identityUser.PhoneNumber);
        Assert.False(identityUser.PhoneNumberConfirmed);
        Assert.Equal("+27821234567", identityUser.PendingPhoneNumber);
    }

    [Fact]
    public async Task Requesting_a_second_number_invalidates_the_code_issued_for_the_first()
    {
        var (accounts, otp, app, tenant, currentUser, userId, _, clock) = await SeedAsync();
        var dispatcher = new CapturingDispatcher();
        var request = BuildUpdateHandler(currentUser, accounts, otp, dispatcher);

        var first = await request.Handle(new UpdatePhoneCommand(new UpdatePhoneRequest("+27820000001")), default);
        Assert.False(first.IsError);
        var codeForFirst = dispatcher.LastCode!;

        clock.Advance(TimeSpan.FromSeconds(90));

        var second = await request.Handle(new UpdatePhoneCommand(new UpdatePhoneRequest("+27820000002")), default);
        Assert.False(second.IsError);
        Assert.NotEqual(codeForFirst, dispatcher.LastCode);

        var verify = new VerifyPhoneCommandHandler(currentUser, accounts, otp, app, tenant);
        var result = await verify.Handle(new VerifyPhoneCommand(new VerifyPhoneRequest(codeForFirst)), default);

        Assert.True(result.IsError);

        await using var authAfter = fixture.CreateAuthDbContext();
        var identityUser = await authAfter.Users.SingleAsync(u => u.Id == userId);
        Assert.Null(identityUser.PhoneNumber);
        Assert.False(identityUser.PhoneNumberConfirmed);
    }

    [Fact]
    public async Task The_code_for_the_latest_number_promotes_that_number()
    {
        var (accounts, otp, app, tenant, currentUser, userId, _, clock) = await SeedAsync();
        var dispatcher = new CapturingDispatcher();
        var request = BuildUpdateHandler(currentUser, accounts, otp, dispatcher);

        await request.Handle(new UpdatePhoneCommand(new UpdatePhoneRequest("+27820000001")), default);

        clock.Advance(TimeSpan.FromSeconds(90));

        await request.Handle(new UpdatePhoneCommand(new UpdatePhoneRequest("+27820000002")), default);
        var codeForSecond = dispatcher.LastCode!;

        var verify = new VerifyPhoneCommandHandler(currentUser, accounts, otp, app, tenant);
        var result = await verify.Handle(new VerifyPhoneCommand(new VerifyPhoneRequest(codeForSecond)), default);

        Assert.False(result.IsError);

        await using var authAfter = fixture.CreateAuthDbContext();
        var identityUser = await authAfter.Users.SingleAsync(u => u.Id == userId);
        Assert.Equal("+27820000002", identityUser.PhoneNumber);
        Assert.True(identityUser.PhoneNumberConfirmed);
    }

    [Fact]
    public async Task Update_phone_returns_challenge_unavailable_when_issuing_is_throttled()
    {
        var (accounts, otp, _, _, currentUser, _, _, _) = await SeedAsync();
        var dispatcher = new CapturingDispatcher();
        var request = BuildUpdateHandler(currentUser, accounts, otp, dispatcher);

        var first = await request.Handle(new UpdatePhoneCommand(new UpdatePhoneRequest("+27820000001")), default);
        Assert.False(first.IsError);

        var second = await request.Handle(new UpdatePhoneCommand(new UpdatePhoneRequest("+27820000002")), default);

        Assert.True(second.IsError);
        Assert.Equal(
            FirearmStudio.Application.Auth.AuthErrorCodes.ChallengeUnavailable,
            second.FirstError.Code);
    }

    [Fact]
    public async Task Update_phone_returns_phone_channel_unavailable_when_whatsapp_is_down()
    {
        var (accounts, otp, _, _, currentUser, _, _, _) = await SeedAsync();

        var dispatcher = new OtpDispatcher(
            new ThrowingEmailSender(),
            new ThrowingWhatsAppSender(),
            NullLogger<OtpDispatcher>.Instance);

        var request = BuildUpdateHandler(currentUser, accounts, otp, dispatcher);
        var result = await request.Handle(
            new UpdatePhoneCommand(new UpdatePhoneRequest("+27821234567")), default);

        Assert.True(result.IsError);
        Assert.Equal(
            FirearmStudio.Application.Auth.AuthErrorCodes.PhoneChannelUnavailable,
            result.FirstError.Code);
    }

    [Fact]
    public async Task An_expired_code_clears_the_pending_number()
    {
        var (accounts, otp, app, tenant, currentUser, userId, _, clock) = await SeedAsync();
        var dispatcher = new CapturingDispatcher();
        var request = BuildUpdateHandler(currentUser, accounts, otp, dispatcher);

        await request.Handle(new UpdatePhoneCommand(new UpdatePhoneRequest("+27821234567")), default);
        var code = dispatcher.LastCode!;

        clock.Advance(TimeSpan.FromMinutes(16));

        var verify = new VerifyPhoneCommandHandler(currentUser, accounts, otp, app, tenant);
        var expired = await verify.Handle(new VerifyPhoneCommand(new VerifyPhoneRequest(code)), default);

        Assert.True(expired.IsError);
        Assert.Equal(
            FirearmStudio.Application.Auth.AuthErrorCodes.CodeExpired,
            expired.FirstError.Code);

        await using (var authAfter = fixture.CreateAuthDbContext())
        {
            var identityUser = await authAfter.Users.SingleAsync(u => u.Id == userId);
            Assert.Null(identityUser.PendingPhoneNumber);
            Assert.Null(identityUser.PhoneNumber);
            Assert.False(identityUser.PhoneNumberConfirmed);
        }

        var reissued = await otp.IssueAsync(userId, OtpPurpose.PhoneChange, default);
        var afterClearing = await verify.Handle(
            new VerifyPhoneCommand(new VerifyPhoneRequest(reissued.Code!)), default);

        Assert.True(afterClearing.IsError);
        Assert.Equal(
            FirearmStudio.Application.Auth.AuthErrorCodes.NoPendingPhoneChange,
            afterClearing.FirstError.Code);
    }

    [Fact]
    public async Task Verify_with_no_pending_change_returns_error()
    {
        var (accounts, otp, app, tenant, currentUser, userId, _, _) = await SeedAsync();

        var issued = await otp.IssueAsync(userId, OtpPurpose.PhoneChange, default);

        var verify = new VerifyPhoneCommandHandler(currentUser, accounts, otp, app, tenant);
        var result = await verify.Handle(new VerifyPhoneCommand(new VerifyPhoneRequest(issued.Code!)), default);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == FirearmStudio.Application.Auth.AuthErrorCodes.NoPendingPhoneChange);
    }
}
