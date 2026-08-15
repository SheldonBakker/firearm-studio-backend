using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Users;
using FirearmStudio.Application.Users.UpdatePhone;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Identity;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        BypassTenantContext Tenant, FixedCurrentUser CurrentUser, Guid UserId, Guid CompanyId)> SeedAsync()
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

        return (accounts, otp, app, tenant, new FixedCurrentUser(userId, email), userId, companyId);
    }

    [Fact]
    public async Task Verify_promotes_pending_and_mirrors_to_app_user()
    {
        var (accounts, otp, app, tenant, currentUser, userId, companyId) = await SeedAsync();
        var dispatcher = new CapturingDispatcher();

        var request = new UpdatePhoneCommandHandler(currentUser, accounts, otp, dispatcher);
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
        var (accounts, otp, app, tenant, currentUser, userId, _) = await SeedAsync();
        var dispatcher = new CapturingDispatcher();

        var request = new UpdatePhoneCommandHandler(currentUser, accounts, otp, dispatcher);
        await request.Handle(new UpdatePhoneCommand(new UpdatePhoneRequest("+27821234567")), default);

        // A code guaranteed to differ from the issued one.
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
    public async Task Verify_with_no_pending_change_returns_error()
    {
        var (accounts, otp, app, tenant, currentUser, userId, _) = await SeedAsync();

        // A code issued for PhoneChange purpose without ever setting a pending phone number,
        // so it verifies successfully but there is nothing for ConfirmPhoneChangeAsync to promote.
        var issued = await otp.IssueAsync(userId, OtpPurpose.PhoneChange, default);

        var verify = new VerifyPhoneCommandHandler(currentUser, accounts, otp, app, tenant);
        var result = await verify.Handle(new VerifyPhoneCommand(new VerifyPhoneRequest(issued.Code!)), default);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == FirearmStudio.Application.Auth.AuthErrorCodes.NoPendingPhoneChange);
    }
}
