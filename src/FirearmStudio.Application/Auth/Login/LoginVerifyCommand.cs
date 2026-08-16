using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Auth.Login;

public sealed record LoginVerifyCommand(LoginVerifyRequest Request) : ICommand<ErrorOr<AuthTokensResponse>>;
