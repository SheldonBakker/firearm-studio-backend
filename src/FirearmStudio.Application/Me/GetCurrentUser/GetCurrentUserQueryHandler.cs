using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Me.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(ICurrentUserService currentUserService)
    : IQueryHandler<GetCurrentUserQuery, ErrorOr<CurrentUserResponse>>
{
    public Task<ErrorOr<CurrentUserResponse>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = currentUserService.User;
        ErrorOr<CurrentUserResponse> response = new CurrentUserResponse(user.Id, user.Email, user.Roles);
        return Task.FromResult(response);
    }
}
