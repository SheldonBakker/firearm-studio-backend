using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Auth.AcceptInvite;

public sealed record AcceptInviteCommand(AcceptInviteRequest Request)
    : ICommand<ErrorOr<AuthTokensResponse>>;
