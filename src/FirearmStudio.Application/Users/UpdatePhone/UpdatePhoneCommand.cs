using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;

namespace FirearmStudio.Application.Users.UpdatePhone;

public sealed record UpdatePhoneCommand(UpdatePhoneRequest Request) : ICommand<ErrorOr<Success>>;
