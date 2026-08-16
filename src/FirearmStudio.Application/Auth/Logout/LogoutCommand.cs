using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Auth.Logout;

public sealed record LogoutCommand(LogoutRequest Request) : ICommand<ErrorOr<Success>>;
