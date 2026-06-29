using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Users.ListUsers;

public sealed class ListUsersQueryHandler(IApplicationDbContext db)
    : IQueryHandler<ListUsersQuery, ErrorOr<IReadOnlyList<AppUserResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<AppUserResponse>>> Handle(
        ListUsersQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppUserResponse> users = await db.AppUsers
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ThenBy(user => user.Id)
            .Select(AppUserResponse.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(users);
    }
}
