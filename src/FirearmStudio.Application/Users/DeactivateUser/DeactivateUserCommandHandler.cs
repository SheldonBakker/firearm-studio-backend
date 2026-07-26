using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Users.DeactivateUser;

public sealed class DeactivateUserCommandHandler(IApplicationDbContext db)
    : ICommandHandler<DeactivateUserCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(
        DeactivateUserCommand command,
        CancellationToken cancellationToken)
    {
        ErrorOr<Success> outcome = Error.Conflict(
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
                && user.IsActive
                && await db.AppUsers.CountAsync(
                    candidate => candidate.Role == AppRole.Admin && candidate.IsActive,
                    ct) <= 1)
            {
                outcome = Error.Conflict(ErrorCodes.LastActiveAdmin, "The company must retain at least one active admin.");
                return;
            }

            user.IsActive = false;
            await db.SaveChangesAsync(ct);

            outcome = Result.Success;
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

    public static class ErrorCodes
    {
        public const string NotFound = "DeactivateUserCommand.NotFound";
        public const string LastActiveAdmin = "DeactivateUserCommand.LastActiveAdmin";
        public const string ConcurrentModification = "DeactivateUserCommand.ConcurrentModification";
    }
}
