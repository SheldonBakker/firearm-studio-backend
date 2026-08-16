using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Auth.VerifyEmail;

public sealed record VerifyEmailCommand(VerifyEmailRequest Request)
    : ICommand<ErrorOr<AuthTokensResponse>>;
