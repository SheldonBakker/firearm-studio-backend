using FirearmStudio.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class IdentityPhoneMethodsTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
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

    private async Task<(IdentityUserAccountService Accounts, AuthDbContext Auth, Guid UserId)> CreateAsync()
    {
        await fixture.MigrateAllAsync();
        var auth = fixture.CreateAuthDbContext();
        var userId = Guid.NewGuid();
        var email = $"{Guid.NewGuid():N}@example.com";

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

        return (new IdentityUserAccountService(BuildUserManager(auth)), auth, userId);
    }

    private static async Task<AppIdentityUser> ReloadAsync(TestDatabaseFixture fixture, Guid userId)
    {
        await using var db = fixture.CreateAuthDbContext();
        return await db.Users.SingleAsync(u => u.Id == userId);
    }

    [Fact]
    public async Task Set_pending_then_confirm_promotes_and_clears()
    {
        var (accounts, _, userId) = await CreateAsync();

        await accounts.SetPendingPhoneNumberAsync(userId, "+27821234567", default);
        var promoted = await accounts.ConfirmPhoneChangeAsync(userId, default);

        Assert.Equal("+27821234567", promoted);
        var user = await ReloadAsync(fixture, userId);
        Assert.Equal("+27821234567", user.PhoneNumber);
        Assert.True(user.PhoneNumberConfirmed);
        Assert.Null(user.PendingPhoneNumber);
    }

    [Fact]
    public async Task Confirm_with_no_pending_returns_null_and_changes_nothing()
    {
        var (accounts, _, userId) = await CreateAsync();

        var promoted = await accounts.ConfirmPhoneChangeAsync(userId, default);

        Assert.Null(promoted);
        var user = await ReloadAsync(fixture, userId);
        Assert.Null(user.PhoneNumber);
        Assert.False(user.PhoneNumberConfirmed);
    }

    [Fact]
    public async Task Set_two_factor_enabled_persists()
    {
        var (accounts, _, userId) = await CreateAsync();

        await accounts.SetTwoFactorEnabledAsync(userId, true, default);

        var user = await ReloadAsync(fixture, userId);
        Assert.True(user.TwoFactorEnabled);
    }

    [Fact]
    public async Task Find_by_email_exposes_two_factor_and_phone_fields()
    {
        var (accounts, _, userId) = await CreateAsync();
        await accounts.SetPhoneNumberAsync(userId, "+27829999999", confirmed: true, default);
        await accounts.SetPendingPhoneNumberAsync(userId, "+27820000002", default);

        var user = await ReloadAsync(fixture, userId);
        var account = await accounts.FindByEmailAsync(user.Email!, default);

        Assert.NotNull(account);
        Assert.Equal("+27829999999", account!.PhoneNumber);
        Assert.Equal("+27820000002", account.PendingPhoneNumber);
    }
}
