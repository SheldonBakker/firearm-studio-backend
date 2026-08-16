using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Auth.Refresh;

public sealed record RefreshCommand(RefreshRequest Request)
    : ICommand<ErrorOr<AuthTokensResponse>>;
