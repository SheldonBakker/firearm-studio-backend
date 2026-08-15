using FirearmStudio.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace FirearmStudio.Infrastructure.Identity;

public sealed class IdentityUserAccountService(UserManager<AppIdentityUser> users)
    : IUserAccountService
{
    public async Task<UserAccount?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email);

        return user is null ? null : Map(user);
    }

    public async Task<(UserAccount? Account, IReadOnlyList<string> Errors)> CreateAsync(
        string email,
        string password,
        CancellationToken ct)
    {
        var user = new AppIdentityUser
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            UserName = email,
        };

        var result = await users.CreateAsync(user, password);

        return result.Succeeded
            ? (Map(user), [])
            : (null, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<PasswordCheckResult> CheckPasswordAsync(
        Guid userId,
        string password,
        CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return PasswordCheckResult.Failed;
        }

        if (await users.IsLockedOutAsync(user))
        {
            return PasswordCheckResult.LockedOut;
        }

        if (await users.CheckPasswordAsync(user, password))
        {
            await users.ResetAccessFailedCountAsync(user);
            return PasswordCheckResult.Succeeded;
        }

        await users.AccessFailedAsync(user);

        return await users.IsLockedOutAsync(user)
            ? PasswordCheckResult.LockedOut
            : PasswordCheckResult.Failed;
    }

    public async Task ConfirmEmailAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || user.EmailConfirmed)
        {
            return;
        }

        user.EmailConfirmed = true;
        await users.UpdateAsync(user);
    }

    public async Task<IReadOnlyList<string>> SetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return ["User not found."];
        }

        await users.RemovePasswordAsync(user);
        var result = await users.AddPasswordAsync(user, newPassword);

        if (!result.Succeeded)
        {
            return result.Errors.Select(e => e.Description).ToList();
        }

        await users.ResetAccessFailedCountAsync(user);
        await users.SetLockoutEndDateAsync(user, null);

        return [];
    }

    private static UserAccount Map(AppIdentityUser user) =>
        new(user.Id, user.Email!, user.EmailConfirmed);
}
