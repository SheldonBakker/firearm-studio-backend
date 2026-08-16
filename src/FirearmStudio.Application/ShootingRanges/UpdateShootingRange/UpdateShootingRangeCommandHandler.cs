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
        request.Name.ApplyTo(v => range.Name = v);
        request.Description.ApplyTo(v => range.Description = v);
        request.LaneCount.ApplyTo(v => range.LaneCount = v);
        request.SlotIntervalMinutes.ApplyTo(v => range.SlotIntervalMinutes = v);
        request.IsActive.ApplyTo(v => range.IsActive = v);
        request.OperatingHours.ApplyTo(hours =>
        {
            db.RangeOperatingHours.RemoveRange(range.OperatingHours);
            range.OperatingHours = hours.Select(h => new RangeOperatingHours
            {
                ShootingRangeId = range.Id,
                Day = h.Day,
                OpenTime = h.OpenTime,
                CloseTime = h.CloseTime,
            }).ToList();
        });

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateShootingRangeCommand.NotFound";
    }
}
