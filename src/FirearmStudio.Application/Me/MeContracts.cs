namespace FirearmStudio.Application.Me;

public sealed record CurrentUserResponse(
    Guid Id,
    string? Email,
    IReadOnlyList<string> Roles,
    bool TwoFactorEnabled,
    string? PhoneNumber);

public sealed record AdminCheckResponse(bool IsAdmin, Guid Id);
