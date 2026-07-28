using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.ExportRegister;

public sealed record ExportRegisterQuery(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    Guid? ShootingRangeId) : IQuery<ErrorOr<byte[]>>;
