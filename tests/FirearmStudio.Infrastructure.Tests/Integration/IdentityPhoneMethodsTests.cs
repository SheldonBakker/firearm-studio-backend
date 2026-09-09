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
    public async Task Set_two_factor_enabled_persists()
    {
        var (accounts, _, userId) = await CreateAsync();

        await accounts.SetTwoFactorEnabledAsync(userId, true, default);

        var user = await ReloadAsync(fixture, userId);
        Assert.True(user.TwoFactorEnabled);
    }

    [Fact]
    public async Task Set_phone_number_persists_and_find_by_email_returns_it()
    {
        var (accounts, _, userId) = await CreateAsync();
        await accounts.SetPhoneNumberAsync(userId, "+27829999999", default);

        var user = await ReloadAsync(fixture, userId);
        var account = await accounts.FindByEmailAsync(user.Email!, default);

        Assert.NotNull(account);
        Assert.Equal("+27829999999", account!.PhoneNumber);
    }
}
