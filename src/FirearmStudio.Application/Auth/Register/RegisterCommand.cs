using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Auth.Register;

public sealed record RegisterCommand(RegisterRequest Request) : ICommand<ErrorOr<Success>>;
