using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Users.VerifyPhone;

public sealed record VerifyPhoneCommand(VerifyPhoneRequest Request) : ICommand<ErrorOr<Success>>;
