using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Users;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Infrastructure.Services;

public sealed class UserManagementService(IApplicationDbContext db) : IUserManagementService
{
    public async Task<ErrorOr<IReadOnlyList<AppUserResponse>>> ListUsersAsync(CancellationToken ct = default)
    {
        var users = await db.AppUsers
            .OrderBy(u => u.Email)
            .ToListAsync(ct);

        return users.Select(Map).ToList();
    }

    public async Task<ErrorOr<AppUserResponse>> InviteUserAsync(InviteUserRequest request, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(request.Role))
        {
            return Error.Validation("Role", "Unknown role.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await db.AppUsers.AnyAsync(u => u.Email == email, ct);
        if (exists)
        {
            return Error.Conflict(description: "A user with this email already exists in your company.");
        }

        var appUser = new AppUser
        {
            Email = email,
            FullName = request.FullName,
            Role = request.Role,
            IsActive = true,
            InvitedAt = DateTime.UtcNow,
        };

        await db.AppUsers.AddAsync(appUser, ct);
        await db.SaveChangesAsync(ct);

        return Map(appUser);
    }

    public async Task<ErrorOr<AppUserResponse>> ChangeRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(request.Role))
        {
            return Error.Validation("Role", "Unknown role.");
        }

        var newRole = request.Role;

        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound(description: "User not found.");
        }

        if (user.Role == AppRole.Admin && newRole != AppRole.Admin && await IsLastActiveAdminAsync(ct))
        {
            return Error.Conflict(description: "The company must retain at least one active admin.");
        }

        user.Role = newRole;
        await db.SaveChangesAsync(ct);

        return Map(user);
    }

    public async Task<ErrorOr<Success>> DeactivateUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound(description: "User not found.");
        }

        if (user.Role == AppRole.Admin && user.IsActive && await IsLastActiveAdminAsync(ct))
        {
            return Error.Conflict(description: "The company must retain at least one active admin.");
        }

        user.IsActive = false;
        await db.SaveChangesAsync(ct);

        return Result.Success;
    }

    private async Task<bool> IsLastActiveAdminAsync(CancellationToken ct)
    {
        var activeAdmins = await db.AppUsers.CountAsync(u => u.Role == AppRole.Admin && u.IsActive, ct);
        return activeAdmins <= 1;
    }

    private static AppUserResponse Map(AppUser u) =>
        new(u.Id, u.Email, u.FullName, u.Role, u.IsActive, u.AuthUserId is not null);
}
