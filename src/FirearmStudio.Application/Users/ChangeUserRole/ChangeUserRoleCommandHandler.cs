using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Users.ChangeUserRole;

public sealed class ChangeUserRoleCommandHandler(IApplicationDbContext db)
    : ICommandHandler<ChangeUserRoleCommand, ErrorOr<AppUserResponse>>
{
    public async Task<ErrorOr<AppUserResponse>> Handle(
        ChangeUserRoleCommand command,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(command.Request.Role))
        {
            return Error.Validation(ErrorCodes.UnknownRole, "Unknown role.");
        }

        ErrorOr<AppUserResponse> outcome = Error.Conflict(
            ErrorCodes.ConcurrentModification,
            "The user was modified by another request. Please retry.");

        var committed = await db.TryExecuteInSerializableTransactionAsync(async ct =>
        {
            var user = await db.AppUsers.FirstOrDefaultAsync(candidate => candidate.Id == command.Id, ct);
            if (user is null)
            {
                outcome = Error.NotFound(ErrorCodes.NotFound, "User not found.");
                return;
            }

            if (user.Role == AppRole.Admin
                && command.Request.Role != AppRole.Admin
                && await IsLastActiveAdminAsync(ct))
            {
                outcome = Error.Conflict(ErrorCodes.LastActiveAdmin, "The company must retain at least one active admin.");
                return;
            }

            user.Role = command.Request.Role;
            await db.SaveChangesAsync(ct);

            outcome = AppUserResponse.FromEntity(user);
        }, cancellationToken);

        if (outcome.IsError)
        {
            return outcome.Errors;
        }

        if (!committed)
        {
            return Error.Conflict(
                ErrorCodes.ConcurrentModification,
                "The user was modified by another request. Please retry.");
        }

        return outcome;
    }

    private async Task<bool> IsLastActiveAdminAsync(CancellationToken cancellationToken)
    {
        var activeAdminCount = await db.AppUsers.CountAsync(
            user => user.Role == AppRole.Admin && user.IsActive,
            cancellationToken);
        return activeAdminCount <= 1;
    }

    public static class ErrorCodes
    {
        public const string UnknownRole = "ChangeUserRoleCommand.UnknownRole";
        public const string NotFound = "ChangeUserRoleCommand.NotFound";
        public const string LastActiveAdmin = "ChangeUserRoleCommand.LastActiveAdmin";
        public const string ConcurrentModification = "ChangeUserRoleCommand.ConcurrentModification";
    }
}
