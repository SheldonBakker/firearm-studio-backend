namespace FirearmStudio.Application.Me;

public sealed record CurrentUserResponse(Guid Id, string? Email, IReadOnlyList<string> Roles);

public sealed record AdminCheckResponse(bool IsAdmin, Guid Id);
