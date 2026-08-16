using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Users.ListUsers;

public sealed class ListUsersQueryHandler(IApplicationDbContext db)
    : IQueryHandler<ListUsersQuery, ErrorOr<PaginatedResponse<AppUserResponse>>>
{
    public async Task<ErrorOr<PaginatedResponse<AppUserResponse>>> Handle(
        ListUsersQuery query,
        CancellationToken cancellationToken)
    {
        var queryable = db.AppUsers
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ThenBy(user => user.Id);

        return await queryable.ToPaginatedAsync(
            query.PageNumber, query.PageSize, AppUserResponse.QueryProjection, cancellationToken);
    }
}
