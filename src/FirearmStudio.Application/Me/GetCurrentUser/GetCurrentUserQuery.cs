using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Me.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<ErrorOr<CurrentUserResponse>>;
