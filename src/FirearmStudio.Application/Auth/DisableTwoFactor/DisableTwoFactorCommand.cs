using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Auth.DisableTwoFactor;

public sealed record DisableTwoFactorCommand(DisableTwoFactorRequest Request) : ICommand<ErrorOr<Success>>;
