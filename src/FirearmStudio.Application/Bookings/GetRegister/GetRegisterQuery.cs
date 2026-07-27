using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;

namespace FirearmStudio.Application.Bookings.GetRegister;

public sealed record GetRegisterQuery(
    int PageNumber,
    int PageSize,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    Guid? ShootingRangeId) : IQuery<ErrorOr<PaginatedResponse<RegisterRowDto>>>;
