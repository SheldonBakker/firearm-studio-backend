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
        var user = await db.AppUsers.FirstOrDefaultAsync(candidate => candidate.Id == command.Id, cancellationToken);
        if (user is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "User not found.");
        }

        if (user.Role == AppRole.Admin
            && user.IsActive
            && await db.AppUsers.CountAsync(
                candidate => candidate.Role == AppRole.Admin && candidate.IsActive,
                cancellationToken) <= 1)
        {
            return Error.Conflict(ErrorCodes.LastActiveAdmin, "The company must retain at least one active admin.");
        }

        user.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "DeactivateUserCommand.NotFound";
        public const string LastActiveAdmin = "DeactivateUserCommand.LastActiveAdmin";
    }
}
