using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Users;
using FirearmStudio.Application.Users.InviteUser;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Identity;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

/// <summary>
/// An Invite code is credential-equivalent, so these tests pin down which destinations an
/// invite may reach. A request-supplied phone is only honoured when the invite creates the
/// login account; otherwise the code must stay on the mailbox the account already owns.
/// </summary>
public sealed class InviteDeliveryTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private sealed class CapturingDispatcher : IOtpDispatcher
    {
        public OtpRecipient? LastRecipient { get; private set; }
        public OtpPurpose? LastPurpose { get; private set; }
        public int Calls { get; private set; }

        public Task SendAsync(OtpRecipient recipient, OtpPurpose purpose, string code, int expiresInMinutes, CancellationToken ct)
        {
            Calls++;
            LastRecipient = recipient;
            LastPurpose = purpose;
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

    private sealed record Harness(
        InviteUserCommandHandler Invite,
        IdentityUserAccountService Accounts,
        CapturingDispatcher Dispatcher,
        ApplicationDbContext App,
        BypassTenantContext Tenant,
        Guid CompanyId);

    private async Task<Harness> CreateAsync()
    {
        await fixture.MigrateAllAsync();

        var clock = new TestTimeProvider(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var auth = fixture.CreateAuthDbContext();
        var tenant = new BypassTenantContext();
        var app = fixture.CreateDbContext(tenant);

        var accounts = new IdentityUserAccountService(BuildUserManager(auth));
        var otp = new OtpService(auth, new PasswordHasher<AppIdentityUser>(), clock);
        var dispatcher = new CapturingDispatcher();

        var companyId = Guid.NewGuid();
        app.Companies.Add(new Company { Id = companyId, Name = "Inviting Co" });
        await app.SaveChangesAsync();
        tenant.CompanyId = companyId;

        return new Harness(
            new InviteUserCommandHandler(app, tenant, accounts, otp, dispatcher),
            accounts,
            dispatcher,
            app,
            tenant,
            companyId);
    }

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Invite_for_a_brand_new_account_uses_the_supplied_phone()
    {
        var h = await CreateAsync();
        var invitee = NewEmail();

        var result = await h.Invite.Handle(
            new InviteUserCommand(new InviteUserRequest(invitee, "New Staffer", AppRole.Staff, "+27821234567")),
            default);

        Assert.False(result.IsError);
        Assert.Equal(1, h.Dispatcher.Calls);
        Assert.Equal(OtpPurpose.Invite, h.Dispatcher.LastPurpose);
        Assert.Equal("+27821234567", h.Dispatcher.LastRecipient!.PhoneNumber);
        Assert.Equal(invitee, h.Dispatcher.LastRecipient.Email);
    }

    [Fact]
    public async Task Invite_for_an_existing_account_never_uses_the_supplied_phone()
    {
        var h = await CreateAsync();
        var victim = NewEmail();

        // The victim already has a login account, so an Invite code is a working credential
        // for it. The invite must not be able to route that code to a caller-chosen number.
        var (created, errors) = await h.Accounts.CreateAsync(victim, "VictimSecret123", default);
        Assert.Empty(errors);
        Assert.NotNull(created);

        var result = await h.Invite.Handle(
            new InviteUserCommand(new InviteUserRequest(victim, "Victim", AppRole.Staff, "+27829999999")),
            default);

        Assert.False(result.IsError);
        Assert.Equal(1, h.Dispatcher.Calls);
        Assert.Equal(OtpPurpose.Invite, h.Dispatcher.LastPurpose);
        Assert.Null(h.Dispatcher.LastRecipient!.PhoneNumber);
        Assert.Equal(victim, h.Dispatcher.LastRecipient.Email);
    }

    [Fact]
    public async Task Reinvite_of_an_unlinked_app_user_still_uses_the_phone()
    {
        var h = await CreateAsync();
        var invitee = NewEmail();

        // An AppUser row with no auth_user_id: invited before, never accepted, so this
        // invite still creates the login account and may seed the supplied number.
        h.App.AppUsers.Add(new AppUser
        {
            CompanyId = h.CompanyId,
            Email = invitee,
            Role = AppRole.Staff,
            IsActive = true,
            InvitedAt = DateTime.UtcNow,
        });
        await h.App.SaveChangesAsync();

        var result = await h.Invite.Handle(
            new InviteUserCommand(new InviteUserRequest(invitee, "Staffer", AppRole.Staff, "+27821112222")),
            default);

        Assert.False(result.IsError);
        Assert.Equal(1, h.Dispatcher.Calls);
        Assert.Equal("+27821112222", h.Dispatcher.LastRecipient!.PhoneNumber);

        await using var appAfter = fixture.CreateDbContext(h.CompanyId);
        var appUser = await appAfter.AppUsers.SingleAsync(u => u.Email == invitee);
        Assert.Equal("+27821112222", appUser.PhoneNumber);
    }
}
