using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Users.ListUsers;

public sealed record ListUsersQuery : IQuery<ErrorOr<IReadOnlyList<AppUserResponse>>>;
