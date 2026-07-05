using System.Linq.Expressions;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.ShootingRanges;

public sealed record OperatingHoursDto(DayOfWeek Day, TimeOnly OpenTime, TimeOnly CloseTime)
{
    public static OperatingHoursDto FromEntity(RangeOperatingHours h) =>
        new(h.Day, h.OpenTime, h.CloseTime);
}

public sealed record ShootingRangeResponse(
    Guid Id,
    string Name,
    string? Description,
    int LaneCount,
    int SlotIntervalMinutes,
    bool IsActive,
    IReadOnlyList<OperatingHoursDto> OperatingHours)
{
    public static Expression<Func<ShootingRange, ShootingRangeResponse>> QueryProjection => r => new ShootingRangeResponse(
        r.Id, r.Name, r.Description, r.LaneCount, r.SlotIntervalMinutes, r.IsActive,
        r.OperatingHours
            .OrderBy(h => h.Day)
            .Select(h => new OperatingHoursDto(h.Day, h.OpenTime, h.CloseTime))
            .ToList());

    public static ShootingRangeResponse FromEntity(ShootingRange r) =>
        new(r.Id, r.Name, r.Description, r.LaneCount, r.SlotIntervalMinutes, r.IsActive,
            r.OperatingHours
                .OrderBy(h => h.Day)
                .Select(OperatingHoursDto.FromEntity)
                .ToList());
}

public sealed record ShootingRangeListItemDto(
    Guid Id,
    string Name,
    int LaneCount,
    int SlotIntervalMinutes,
    bool IsActive)
{
    public static Expression<Func<ShootingRange, ShootingRangeListItemDto>> QueryProjection => r => new ShootingRangeListItemDto(
        r.Id, r.Name, r.LaneCount, r.SlotIntervalMinutes, r.IsActive);
}

public sealed record PublicRangeResponse(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<OperatingHoursDto> OperatingHours)
{
    public static Expression<Func<ShootingRange, PublicRangeResponse>> QueryProjection => r => new PublicRangeResponse(
        r.Id, r.Name, r.Description,
        r.OperatingHours
            .OrderBy(h => h.Day)
            .Select(h => new OperatingHoursDto(h.Day, h.OpenTime, h.CloseTime))
            .ToList());
}

public sealed record CreateShootingRangeRequest(
    string Name,
    string? Description,
    int LaneCount,
    int SlotIntervalMinutes,
    IReadOnlyList<OperatingHoursDto> OperatingHours);

public sealed record UpdateShootingRangeRequest(
    Optional<string> Name,
    Optional<string?> Description,
    Optional<int> LaneCount,
    Optional<int> SlotIntervalMinutes,
    Optional<bool> IsActive,
    Optional<IReadOnlyList<OperatingHoursDto>> OperatingHours);
