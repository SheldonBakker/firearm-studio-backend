using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Users.InviteUser;

public sealed class InviteUserCommandHandler(IUserManagementService userManagementService)
    : ICommandHandler<InviteUserCommand, ErrorOr<AppUserResponse>>
{
    public async Task<ErrorOr<AppUserResponse>> Handle(InviteUserCommand command, CancellationToken cancellationToken) =>
        await userManagementService.InviteUserAsync(command.Request, cancellationToken);
}
