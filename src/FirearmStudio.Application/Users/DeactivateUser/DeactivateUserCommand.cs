using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Users.DeactivateUser;

public sealed record DeactivateUserCommand(Guid Id) : ICommand<ErrorOr<Success>>;
