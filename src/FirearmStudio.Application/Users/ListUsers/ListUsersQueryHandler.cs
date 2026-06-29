using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Users.ListUsers;

public sealed class ListUsersQueryHandler(IApplicationDbContext db)
    : IQueryHandler<ListUsersQuery, ErrorOr<PaginatedResponse<AppUserResponse>>>
{
    private const int MaxPageSize = 200;

    public async Task<ErrorOr<PaginatedResponse<AppUserResponse>>> Handle(
        ListUsersQuery query,
        CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

        var queryable = db.AppUsers
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ThenBy(user => user.Id);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(AppUserResponse.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<AppUserResponse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
