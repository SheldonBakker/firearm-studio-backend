using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Auth.EnableTwoFactor;

public sealed record EnableTwoFactorCommand : ICommand<ErrorOr<Success>>;
