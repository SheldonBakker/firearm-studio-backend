using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Auth;
using FirearmStudio.Application.Auth.AcceptInvite;
using FirearmStudio.Application.Auth.DisableTwoFactor;
using FirearmStudio.Application.Auth.EnableTwoFactor;
using FirearmStudio.Application.Auth.ForgotPassword;
using FirearmStudio.Application.Auth.Login;
using FirearmStudio.Application.Auth.Logout;
using FirearmStudio.Application.Auth.Refresh;
using FirearmStudio.Application.Auth.Register;
using FirearmStudio.Application.Auth.ResetPassword;
using FirearmStudio.Application.Auth.VerifyEmail;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Application.Users;
using FirearmStudio.Application.Users.InviteUser;
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
using Microsoft.Extensions.Options;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly List<(string Email, OtpPurpose Purpose, string Code)> _sent = [];

    public string LastCodeFor(string email, OtpPurpose purpose) =>
        _sent.Last(s => s.Email == email && s.Purpose == purpose).Code;

    public Task SendOtpAsync(
        string email,
        string? name,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct)
    {
        _sent.Add((email, purpose, code));
        return Task.CompletedTask;
    }
}

public sealed class AuthJourneyTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private sealed class FixedUser(Guid id) : ICurrentUserService
    {
        public CurrentUser User { get; } = new() { Id = id, IsAuthenticated = true };
    }

    private static readonly JwtSettings Settings = new()
    {
        Issuer = "https://api.test.local",
        Audience = "firearm-studio",
        SigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256-abcdef",
    };

    private const string Password = "CorrectHorse123";

    private sealed record Harness(
        RegisterCommandHandler Register,
        VerifyEmailCommandHandler Verify,
        LoginCommandHandler Login,
        RefreshCommandHandler Refresh,
        LogoutCommandHandler Logout,
        ForgotPasswordCommandHandler Forgot,
        ResetPasswordCommandHandler Reset,
        CapturingEmailSender Email,
        TestTimeProvider Clock,
        AcceptInviteCommandHandler AcceptInvite,
        InviteUserCommandHandler Invite,
        ApplicationDbContext App,
        BypassTenantContext Tenant,
        AuthDbContext Auth,
        IdentityUserAccountService Accounts);

    private async Task<Harness> CreateAsync()
    {
        await fixture.MigrateAllAsync();

        var clock = new TestTimeProvider(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var auth = fixture.CreateAuthDbContext();
        var tenant = new BypassTenantContext();
        var app = fixture.CreateDbContext(tenant);

        var userManager = BuildUserManager(auth);
        var accounts = new IdentityUserAccountService(userManager);
        var otp = new OtpService(auth, new PasswordHasher<AppIdentityUser>(), clock);
        var tokens = new TokenService(auth, app, Settings, clock);
        var email = new CapturingEmailSender();
        var dispatcher = new OtpDispatcher(email, new NullWhatsAppSender(), NullLogger<OtpDispatcher>.Instance);

        return new Harness(
            new RegisterCommandHandler(accounts, otp, dispatcher),
            new VerifyEmailCommandHandler(accounts, otp, tokens, app, tenant),
            new LoginCommandHandler(accounts, tokens, otp, dispatcher),
            new RefreshCommandHandler(tokens),
            new LogoutCommandHandler(tokens),
            new ForgotPasswordCommandHandler(accounts, otp, dispatcher),
            new ResetPasswordCommandHandler(accounts, otp, tokens),
            email,
            clock,
            new AcceptInviteCommandHandler(accounts, otp, tokens, app, tenant),
            new InviteUserCommandHandler(app, tenant, accounts, otp, dispatcher),
            app,
            tenant,
            auth,
            accounts);
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
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        }).AddEntityFrameworkStores<AuthDbContext>();

        return services.BuildServiceProvider().GetRequiredService<UserManager<AppIdentityUser>>();
    }

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Register_verify_login_refresh_logout()
    {
        var h = await CreateAsync();
        var email = NewEmail();

        var registered = await h.Register.Handle(
            new RegisterCommand(new RegisterRequest(email, Password)), default);
        Assert.False(registered.IsError);

        var code = h.Email.LastCodeFor(email, OtpPurpose.EmailConfirmation);

        var verified = await h.Verify.Handle(
            new VerifyEmailCommand(new VerifyEmailRequest(email, code)), default);
        Assert.False(verified.IsError);
        Assert.NotEmpty(verified.Value.AccessToken);

        var loggedIn = await h.Login.Handle(
            new LoginCommand(new LoginRequest(email, Password)), default);
        Assert.False(loggedIn.IsError);

        var refreshed = await h.Refresh.Handle(
            new RefreshCommand(new RefreshRequest(loggedIn.Value.Tokens!.RefreshToken)), default);
        Assert.False(refreshed.IsError);
        Assert.NotEqual(loggedIn.Value.Tokens!.RefreshToken, refreshed.Value.RefreshToken);

        var loggedOut = await h.Logout.Handle(
            new LogoutCommand(new LogoutRequest(refreshed.Value.RefreshToken)), default);
        Assert.False(loggedOut.IsError);

        var afterLogout = await h.Refresh.Handle(
            new RefreshCommand(new RefreshRequest(refreshed.Value.RefreshToken)), default);
        Assert.True(afterLogout.IsError);
    }

    [Fact]
    public async Task Login_is_refused_until_the_address_is_confirmed()
    {
        var h = await CreateAsync();
        var email = NewEmail();

        await h.Register.Handle(new RegisterCommand(new RegisterRequest(email, Password)), default);

        var result = await h.Login.Handle(
            new LoginCommand(new LoginRequest(email, Password)), default);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrorCodes.EmailNotConfirmed, result.FirstError.Code);
    }

    [Fact]
    public async Task First_token_carries_no_company_because_onboarding_has_not_run()
    {
        var h = await CreateAsync();
        var email = NewEmail();

        await h.Register.Handle(new RegisterCommand(new RegisterRequest(email, Password)), default);
        var code = h.Email.LastCodeFor(email, OtpPurpose.EmailConfirmation);

        var verified = await h.Verify.Handle(
            new VerifyEmailCommand(new VerifyEmailRequest(email, code)), default);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(verified.Value.AccessToken);

        Assert.DoesNotContain(token.Claims, c => c.Type == AppClaimTypes.CompanyId);
    }

    [Fact]
    public async Task Wrong_password_is_rejected_without_saying_which_field_was_wrong()
    {
        var h = await CreateAsync();
        var email = NewEmail();

        await h.Register.Handle(new RegisterCommand(new RegisterRequest(email, Password)), default);
        var code = h.Email.LastCodeFor(email, OtpPurpose.EmailConfirmation);
        await h.Verify.Handle(new VerifyEmailCommand(new VerifyEmailRequest(email, code)), default);

        var wrongPassword = await h.Login.Handle(
            new LoginCommand(new LoginRequest(email, "WrongPassword123")), default);

        var unknownAddress = await h.Login.Handle(
            new LoginCommand(new LoginRequest(NewEmail(), Password)), default);

        Assert.True(wrongPassword.IsError);
        Assert.True(unknownAddress.IsError);
        Assert.Equal(wrongPassword.FirstError.Code, unknownAddress.FirstError.Code);
        Assert.Equal(wrongPassword.FirstError.Description, unknownAddress.FirstError.Description);
    }

    [Fact]
    public async Task Forgot_password_answers_identically_for_known_and_unknown_addresses()
    {
        var h = await CreateAsync();
        var known = NewEmail();

        await h.Register.Handle(new RegisterCommand(new RegisterRequest(known, Password)), default);

        var forKnown = await h.Forgot.Handle(
            new ForgotPasswordCommand(new ForgotPasswordRequest(known)), default);

        var forUnknown = await h.Forgot.Handle(
            new ForgotPasswordCommand(new ForgotPasswordRequest(NewEmail())), default);

        Assert.False(forKnown.IsError);
        Assert.False(forUnknown.IsError);
    }

    [Fact]
    public async Task Password_reset_works_and_kills_existing_sessions()
    {
        var h = await CreateAsync();
        var email = NewEmail();
        const string newPassword = "BrandNewSecret456";

        await h.Register.Handle(new RegisterCommand(new RegisterRequest(email, Password)), default);
        var confirmCode = h.Email.LastCodeFor(email, OtpPurpose.EmailConfirmation);
        var session = await h.Verify.Handle(
            new VerifyEmailCommand(new VerifyEmailRequest(email, confirmCode)), default);

        await h.Forgot.Handle(new ForgotPasswordCommand(new ForgotPasswordRequest(email)), default);
        var resetCode = h.Email.LastCodeFor(email, OtpPurpose.PasswordReset);

        var reset = await h.Reset.Handle(
            new ResetPasswordCommand(new ResetPasswordRequest(email, resetCode, newPassword)),
            default);
        Assert.False(reset.IsError);

        var oldSession = await h.Refresh.Handle(
            new RefreshCommand(new RefreshRequest(session.Value.RefreshToken)), default);
        Assert.True(oldSession.IsError);

        var withOld = await h.Login.Handle(
            new LoginCommand(new LoginRequest(email, Password)), default);
        Assert.True(withOld.IsError);

        var withNew = await h.Login.Handle(
            new LoginCommand(new LoginRequest(email, newPassword)), default);
        Assert.False(withNew.IsError);
    }

    [Fact]
    public async Task Invited_user_accepts_and_their_first_token_names_the_company()
    {
        var h = await CreateAsync();
        var companyId = Guid.NewGuid();
        var invitee = NewEmail();
        const string chosenPassword = "InviteeSecret789";

        h.App.Companies.Add(new Company { Id = companyId, Name = "Inviting Co" });
        await h.App.SaveChangesAsync();
        h.Tenant.CompanyId = companyId;

        var invited = await h.Invite.Handle(
            new InviteUserCommand(new InviteUserRequest(invitee, "New Staffer", AppRole.Staff)),
            default);
        Assert.False(invited.IsError);
        Assert.False(invited.Value.IsLinked);

        var code = h.Email.LastCodeFor(invitee, OtpPurpose.Invite);

        var accepted = await h.AcceptInvite.Handle(
            new AcceptInviteCommand(new AcceptInviteRequest(invitee, code, chosenPassword)),
            default);
        Assert.False(accepted.IsError);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(accepted.Value.AccessToken);
        Assert.Equal(
            companyId.ToString(),
            token.Claims.First(c => c.Type == AppClaimTypes.CompanyId).Value);
        Assert.Equal("staff", token.Claims.First(c => c.Type == ClaimTypes.Role).Value);

        var loggedIn = await h.Login.Handle(
            new LoginCommand(new LoginRequest(invitee, chosenPassword)), default);
        Assert.False(loggedIn.IsError);
    }

    [Fact]
    public async Task Accepting_an_invite_leaves_an_already_confirmed_phone_untouched()
    {
        var h = await CreateAsync();
        var companyId = Guid.NewGuid();
        var invitee = NewEmail();

        h.App.Companies.Add(new Company { Id = companyId, Name = "Inviting Co" });
        await h.App.SaveChangesAsync();
        h.Tenant.CompanyId = companyId;

        await h.Invite.Handle(
            new InviteUserCommand(new InviteUserRequest(invitee, "New Staffer", AppRole.Staff)), default);

        var seeded = await h.Auth.Users.SingleAsync(u => u.Email == invitee);
        seeded.PhoneNumber = "+27820000001";
        seeded.PhoneNumberConfirmed = true;
        await h.Auth.SaveChangesAsync();

        var code = h.Email.LastCodeFor(invitee, OtpPurpose.Invite);

        var accepted = await h.AcceptInvite.Handle(
            new AcceptInviteCommand(new AcceptInviteRequest(invitee, code, "InviteeSecret789", "+27829999999")),
            default);
        Assert.False(accepted.IsError);

        await using var authAfter = fixture.CreateAuthDbContext();
        var after = await authAfter.Users.SingleAsync(u => u.Email == invitee);
        Assert.Equal("+27820000001", after.PhoneNumber);
        Assert.True(after.PhoneNumberConfirmed);

        await using var appAfter = fixture.CreateDbContext(companyId);
        var appUser = await appAfter.AppUsers.SingleAsync(u => u.Email == invitee);
        Assert.NotEqual("+27829999999", appUser.PhoneNumber);
    }

    [Fact]
    public async Task Accepting_an_invite_seeds_an_unconfirmed_phone_when_none_is_proven()
    {
        var h = await CreateAsync();
        var companyId = Guid.NewGuid();
        var invitee = NewEmail();

        h.App.Companies.Add(new Company { Id = companyId, Name = "Inviting Co" });
        await h.App.SaveChangesAsync();
        h.Tenant.CompanyId = companyId;

        await h.Invite.Handle(
            new InviteUserCommand(new InviteUserRequest(invitee, "New Staffer", AppRole.Staff)), default);

        var code = h.Email.LastCodeFor(invitee, OtpPurpose.Invite);

        var accepted = await h.AcceptInvite.Handle(
            new AcceptInviteCommand(new AcceptInviteRequest(invitee, code, "InviteeSecret789", "+27829999999")),
            default);
        Assert.False(accepted.IsError);

        await using var authAfter = fixture.CreateAuthDbContext();
        var after = await authAfter.Users.SingleAsync(u => u.Email == invitee);
        Assert.Equal("+27829999999", after.PhoneNumber);
        Assert.False(after.PhoneNumberConfirmed);

        await using var appAfter = fixture.CreateDbContext(companyId);
        var appUser = await appAfter.AppUsers.SingleAsync(u => u.Email == invitee);
        Assert.Equal("+27829999999", appUser.PhoneNumber);
    }

    [Fact]
    public async Task Invite_alone_does_not_yield_a_usable_login()
    {
        var h = await CreateAsync();
        var companyId = Guid.NewGuid();
        var invitee = NewEmail();

        h.App.Companies.Add(new Company { Id = companyId, Name = "Inviting Co" });
        await h.App.SaveChangesAsync();
        h.Tenant.CompanyId = companyId;

        await h.Invite.Handle(
            new InviteUserCommand(new InviteUserRequest(invitee, null, AppRole.Viewer)), default);

        var attempt = await h.Login.Handle(
            new LoginCommand(new LoginRequest(invitee, "AnyPasswordAtAll1")), default);

        Assert.True(attempt.IsError);
    }

    [Fact]
    public async Task Invited_user_who_registers_directly_is_still_linked_on_confirmation()
    {
        var h = await CreateAsync();
        var companyId = Guid.NewGuid();
        var invitee = NewEmail();

        h.App.Companies.Add(new Company { Id = companyId, Name = "Inviting Co" });
        await h.App.SaveChangesAsync();
        h.Tenant.CompanyId = companyId;

        h.App.AppUsers.Add(new AppUser
        {
            CompanyId = companyId,
            Email = invitee,
            Role = AppRole.Manager,
            IsActive = true,
            InvitedAt = DateTime.UtcNow,
        });
        await h.App.SaveChangesAsync();

        await h.Register.Handle(new RegisterCommand(new RegisterRequest(invitee, Password)), default);
        var code = h.Email.LastCodeFor(invitee, OtpPurpose.EmailConfirmation);

        var verified = await h.Verify.Handle(
            new VerifyEmailCommand(new VerifyEmailRequest(invitee, code)), default);
        Assert.False(verified.IsError);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(verified.Value.AccessToken);
        Assert.Equal(
            companyId.ToString(),
            token.Claims.First(c => c.Type == AppClaimTypes.CompanyId).Value);
    }

    [Fact]
    public async Task Disabling_two_factor_requires_the_account_password()
    {
        var h = await CreateAsync();
        var email = NewEmail();

        await h.Register.Handle(new RegisterCommand(new RegisterRequest(email, Password)), default);
        var code = h.Email.LastCodeFor(email, OtpPurpose.EmailConfirmation);
        await h.Verify.Handle(new VerifyEmailCommand(new VerifyEmailRequest(email, code)), default);

        var account = await h.Accounts.FindByEmailAsync(email, default);
        var currentUser = new FixedUser(account!.Id);

        var enable = new EnableTwoFactorCommandHandler(currentUser, h.Accounts);
        Assert.False((await enable.Handle(new EnableTwoFactorCommand(), default)).IsError);

        var disable = new DisableTwoFactorCommandHandler(currentUser, h.Accounts);

        var refused = await disable.Handle(
            new DisableTwoFactorCommand(new DisableTwoFactorRequest("NotThePassword1")), default);

        Assert.True(refused.IsError);
        Assert.Equal(AuthErrorCodes.InvalidCredentials, refused.FirstError.Code);
        Assert.True((await h.Accounts.FindByEmailAsync(email, default))!.TwoFactorEnabled);

        var accepted = await disable.Handle(
            new DisableTwoFactorCommand(new DisableTwoFactorRequest(Password)), default);

        Assert.False(accepted.IsError);
        Assert.False((await h.Accounts.FindByEmailAsync(email, default))!.TwoFactorEnabled);
    }

    [Fact]
    public async Task Registering_an_existing_address_reveals_nothing()
    {
        var h = await CreateAsync();
        var email = NewEmail();

        var first = await h.Register.Handle(
            new RegisterCommand(new RegisterRequest(email, Password)), default);

        var second = await h.Register.Handle(
            new RegisterCommand(new RegisterRequest(email, "DifferentPass789")), default);

        Assert.False(first.IsError);
        Assert.False(second.IsError);

        var code = h.Email.LastCodeFor(email, OtpPurpose.EmailConfirmation);
        await h.Verify.Handle(new VerifyEmailCommand(new VerifyEmailRequest(email, code)), default);

        var withSecond = await h.Login.Handle(
            new LoginCommand(new LoginRequest(email, "DifferentPass789")), default);
        Assert.True(withSecond.IsError);

        var withFirst = await h.Login.Handle(
            new LoginCommand(new LoginRequest(email, Password)), default);
        Assert.False(withFirst.IsError);
    }
}
