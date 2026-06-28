using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Firearms.GetFirearms;

public sealed record GetFirearmsQuery(
    string? SerialNumber,
    FirearmStatus? Status,
    string? CustomerName
) : IQuery<ErrorOr<IReadOnlyList<FirearmResponse>>>;
