using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.ShootingRanges.DeleteShootingRange;

public sealed class DeleteShootingRangeCommandHandler(IApplicationDbContext db)
    : ICommandHandler<DeleteShootingRangeCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        DeleteShootingRangeCommand command, CancellationToken cancellationToken)
    {
        var range = await db.ShootingRanges
            .Include(r => r.OperatingHours)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (range is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Shooting range not found.");
        }

        var hasBookings = await db.Bookings.AnyAsync(b => b.ShootingRangeId == command.Id, cancellationToken);
        if (hasBookings)
        {
            return Error.Conflict(ErrorCodes.HasBookings, "The shooting range cannot be deleted while bookings reference it.");
        }

        db.ShootingRanges.Remove(range);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "DeleteShootingRangeCommand.NotFound";
        public const string HasBookings = "DeleteShootingRangeCommand.HasBookings";
    }
}
