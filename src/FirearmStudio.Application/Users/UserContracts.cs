using ErrorOr;

namespace FirearmStudio.Application.Users;

public sealed record InviteUserRequest(string Email, string? FullName, string Role);

public sealed record UpdateUserRoleRequest(string Role);

public sealed record AppUserResponse(
    Guid Id,
    string Email,
    string? FullName,
    string Role,
    bool IsActive,
    bool IsLinked);

public interface IUserManagementService
{
    Task<ErrorOr<IReadOnlyList<AppUserResponse>>> ListUsersAsync(CancellationToken ct = default);

    Task<ErrorOr<AppUserResponse>> InviteUserAsync(InviteUserRequest request, CancellationToken ct = default);

    Task<ErrorOr<AppUserResponse>> ChangeRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken ct = default);

    Task<ErrorOr<Success>> DeactivateUserAsync(Guid userId, CancellationToken ct = default);
}
