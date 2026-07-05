using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.ShootingRanges.CreateShootingRange;

public sealed class CreateShootingRangeCommandHandler(IApplicationDbContext db)
    : ICommandHandler<CreateShootingRangeCommand, ErrorOr<ShootingRangeResponse>>
{
    public async Task<ErrorOr<ShootingRangeResponse>> Handle(
        CreateShootingRangeCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var rangeId = Guid.CreateVersion7();
        var range = new ShootingRange
        {
            Id = rangeId,
            Name = request.Name,
            Description = request.Description,
            LaneCount = request.LaneCount,
            SlotIntervalMinutes = request.SlotIntervalMinutes,
            OperatingHours = request.OperatingHours
                .Select(hours => new RangeOperatingHours
                {
                    ShootingRangeId = rangeId,
                    Day = hours.Day,
                    OpenTime = hours.OpenTime,
                    CloseTime = hours.CloseTime,
                })
                .ToList(),
        };

        await db.ShootingRanges.AddAsync(range, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ShootingRangeResponse.FromEntity(range);
    }
}
