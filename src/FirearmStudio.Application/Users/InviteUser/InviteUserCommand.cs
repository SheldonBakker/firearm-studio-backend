using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Users.InviteUser;

public sealed record InviteUserCommand(InviteUserRequest Request) : ICommand<ErrorOr<AppUserResponse>>;
