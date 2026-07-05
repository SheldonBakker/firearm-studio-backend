using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.GetMonthAvailability;

public sealed record GetMonthAvailabilityQuery(
    Guid? CompanyId,
    Guid ShootingRangeId,
    Guid PackageId,
    int Year,
    int Month) : IQuery<ErrorOr<MonthAvailabilityResponse>>;
