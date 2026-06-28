using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Users.ListUsers;

public sealed class ListUsersQueryHandler(IUserManagementService userManagementService)
    : IQueryHandler<ListUsersQuery, ErrorOr<IReadOnlyList<AppUserResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<AppUserResponse>>> Handle(ListUsersQuery query, CancellationToken cancellationToken) =>
        await userManagementService.ListUsersAsync(cancellationToken);
}
