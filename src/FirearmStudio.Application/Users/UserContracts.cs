using System.Linq.Expressions;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Users;

public sealed record InviteUserRequest(string Email, string? FullName, AppRole Role, string? PhoneNumber = null);

public sealed record UpdateUserRoleRequest(AppRole Role);

public sealed record UpdatePhoneRequest(string PhoneNumber);

public sealed record VerifyPhoneRequest(string Code);

public sealed record AppUserResponse(
    Guid Id,
    string Email,
    string? FullName,
    AppRole Role,
    bool IsActive,
    bool IsLinked,
    string? PhoneNumber)
{
    public static Expression<Func<AppUser, AppUserResponse>> QueryProjection => user => new AppUserResponse(
        user.Id,
        user.Email,
        user.FullName,
        user.Role,
        user.IsActive,
        user.AuthUserId != null,
        user.PhoneNumber);

    public static AppUserResponse FromEntity(AppUser user) =>
        new(user.Id, user.Email, user.FullName, user.Role, user.IsActive, user.AuthUserId is not null, user.PhoneNumber);
}
