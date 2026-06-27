namespace FirearmStudio.Domain.Authentication;

public sealed record CurrentUser
{
    public required Guid Id { get; init; }

    public string? Email { get; init; }

    public Guid? CompanyId { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public bool IsAuthenticated { get; init; }

    public static readonly CurrentUser Anonymous = new()
    {
        Id = Guid.Empty,
        IsAuthenticated = false,
    };

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
