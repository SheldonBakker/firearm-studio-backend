namespace FirearmStudio.Application.Abstractions;

public sealed record UserAccount(Guid Id, string Email, bool EmailConfirmed);

public enum PasswordCheckResult
{
    Succeeded,
    Failed,
    LockedOut,
}

public interface IUserAccountService
{
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken ct);

    Task<(UserAccount? Account, IReadOnlyList<string> Errors)> CreateAsync(
        string email,
        string password,
        CancellationToken ct);

    Task<PasswordCheckResult> CheckPasswordAsync(
        Guid userId,
        string password,
        CancellationToken ct);

    Task ConfirmEmailAsync(Guid userId, CancellationToken ct);

    Task<IReadOnlyList<string>> SetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken ct);
}
