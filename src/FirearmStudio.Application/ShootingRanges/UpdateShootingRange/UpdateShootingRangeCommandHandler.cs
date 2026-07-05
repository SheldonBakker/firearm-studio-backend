using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.ShootingRanges.UpdateShootingRange;

public sealed class UpdateShootingRangeCommandHandler(IApplicationDbContext db)
    : ICommandHandler<UpdateShootingRangeCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(
        UpdateShootingRangeCommand command, CancellationToken cancellationToken)
    {
        var range = await db.ShootingRanges
            .Include(r => r.OperatingHours)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (range is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Shooting range not found.");
        }

        var request = command.Request;

        if (request.Name.IsSet)
        {
            range.Name = request.Name.Value;
        }

        if (request.Description.IsSet)
        {
            range.Description = request.Description.Value;
        }

        if (request.LaneCount.IsSet)
        {
            range.LaneCount = request.LaneCount.Value;
        }

        if (request.SlotIntervalMinutes.IsSet)
        {
            range.SlotIntervalMinutes = request.SlotIntervalMinutes.Value;
        }

        if (request.IsActive.IsSet)
        {
            range.IsActive = request.IsActive.Value;
        }

        if (request.OperatingHours.IsSet)
        {
            db.RangeOperatingHours.RemoveRange(range.OperatingHours);
            range.OperatingHours = request.OperatingHours.Value
                .Select(hours => new RangeOperatingHours
                {
                    ShootingRangeId = range.Id,
                    Day = hours.Day,
                    OpenTime = hours.OpenTime,
                    CloseTime = hours.CloseTime,
                })
                .ToList();
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateShootingRangeCommand.NotFound";
    }
}
