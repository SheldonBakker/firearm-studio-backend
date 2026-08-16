using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Auth.ResetPassword;

public sealed record ResetPasswordCommand(ResetPasswordRequest Request)
    : ICommand<ErrorOr<Success>>;
