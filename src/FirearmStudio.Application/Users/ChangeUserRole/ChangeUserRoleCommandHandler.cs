using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Users.ChangeUserRole;

public sealed class ChangeUserRoleCommandHandler(IUserManagementService userManagementService)
    : ICommandHandler<ChangeUserRoleCommand, ErrorOr<AppUserResponse>>
{
    public async Task<ErrorOr<AppUserResponse>> Handle(ChangeUserRoleCommand command, CancellationToken cancellationToken) =>
        await userManagementService.ChangeRoleAsync(command.Id, command.Request, cancellationToken);
}
