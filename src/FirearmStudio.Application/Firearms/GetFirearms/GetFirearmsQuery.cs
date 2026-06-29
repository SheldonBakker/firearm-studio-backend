using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Firearms.GetFirearms;

public sealed record GetFirearmsQuery(
    int PageNumber,
    int PageSize,
    string? SerialNumber,
    FirearmStatus? Status,
    string? CustomerName
) : IQuery<ErrorOr<PaginatedResponse<FirearmResponse>>>;
