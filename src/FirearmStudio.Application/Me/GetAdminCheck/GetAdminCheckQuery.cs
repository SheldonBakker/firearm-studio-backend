using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Me.GetAdminCheck;

public sealed record GetAdminCheckQuery : IQuery<ErrorOr<AdminCheckResponse>>;
