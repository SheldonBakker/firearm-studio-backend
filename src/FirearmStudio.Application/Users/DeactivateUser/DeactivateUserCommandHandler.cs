using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Users.DeactivateUser;

public sealed class DeactivateUserCommandHandler(IUserManagementService userManagementService)
    : ICommandHandler<DeactivateUserCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(DeactivateUserCommand command, CancellationToken cancellationToken) =>
        await userManagementService.DeactivateUserAsync(command.Id, cancellationToken);
}
