using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Users.InviteUser;

public sealed class InviteUserCommandHandler(
    IApplicationDbContext db,
    ITenantContext tenant)
    : ICommandHandler<InviteUserCommand, ErrorOr<AppUserResponse>>
{
    public async Task<ErrorOr<AppUserResponse>> Handle(
        InviteUserCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (!Enum.IsDefined(request.Role))
        {
            return Error.Validation(ErrorCodes.UnknownRole, "Unknown role.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        bool emailAlreadyExists;
        using (tenant.BeginBypass())
        {
            emailAlreadyExists = await db.AppUsers
                .IgnoreQueryFilters()
                .AnyAsync(user => user.Email == email, cancellationToken);
        }

        if (emailAlreadyExists)
        {
            return Error.Conflict(ErrorCodes.EmailAlreadyExists, "A user with this email already belongs to a company or has a pending invite.");
        }

        var user = new AppUser
        {
            Email = email,
            FullName = request.FullName,
            Role = request.Role,
            IsActive = true,
            InvitedAt = DateTime.UtcNow,
        };

        await db.AppUsers.AddAsync(user, cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Error.Conflict(ErrorCodes.EmailAlreadyExists, "A user with this email already belongs to a company or has a pending invite.");
        }

        return AppUserResponse.FromEntity(user);
    }

    public static class ErrorCodes
    {
        public const string UnknownRole = "InviteUserCommand.UnknownRole";
        public const string EmailAlreadyExists = "InviteUserCommand.EmailAlreadyExists";
    }
}
