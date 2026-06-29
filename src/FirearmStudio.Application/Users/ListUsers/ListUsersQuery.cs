using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;

namespace FirearmStudio.Application.Users.ListUsers;

public sealed record ListUsersQuery(
    int PageNumber,
    int PageSize
) : IQuery<ErrorOr<PaginatedResponse<AppUserResponse>>>;
