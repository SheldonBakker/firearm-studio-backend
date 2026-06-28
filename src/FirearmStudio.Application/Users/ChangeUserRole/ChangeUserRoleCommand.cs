using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Users.ChangeUserRole;

public sealed record ChangeUserRoleCommand(Guid Id, UpdateUserRoleRequest Request) : ICommand<ErrorOr<AppUserResponse>>;
