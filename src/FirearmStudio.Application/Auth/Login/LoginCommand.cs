using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Auth.Login;

public sealed record LoginCommand(LoginRequest Request) : ICommand<ErrorOr<LoginOutcome>>;
