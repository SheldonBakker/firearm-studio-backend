using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Auth.ResendCode;

public sealed record ResendCodeCommand(ResendCodeRequest Request) : ICommand<ErrorOr<Success>>;
