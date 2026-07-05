using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.GetDayAvailability;

public sealed record GetDayAvailabilityQuery(
    Guid? CompanyId,
    Guid ShootingRangeId,
    Guid PackageId,
    DateOnly Date) : IQuery<ErrorOr<DayAvailabilityResponse>>;
