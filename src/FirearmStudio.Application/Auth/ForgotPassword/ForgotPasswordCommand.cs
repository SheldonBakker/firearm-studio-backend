using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(ForgotPasswordRequest Request)
    : ICommand<ErrorOr<Success>>;
