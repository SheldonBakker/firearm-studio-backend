namespace FirearmStudio.Application.Abstractions;

public sealed record UserAccount(
    Guid Id,
    string Email,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    string? PhoneNumber,
    string? PendingPhoneNumber);

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

    Task SetTwoFactorEnabledAsync(Guid userId, bool enabled, CancellationToken ct);

    Task SetPhoneNumberAsync(Guid userId, string? phoneE164, bool confirmed, CancellationToken ct);

    Task SetPendingPhoneNumberAsync(Guid userId, string phoneE164, CancellationToken ct);

    Task<string?> ConfirmPhoneChangeAsync(Guid userId, CancellationToken ct);
}
