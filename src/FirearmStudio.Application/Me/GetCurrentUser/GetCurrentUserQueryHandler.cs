using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Me.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUserService currentUserService,
    IUserAccountService userAccountService)
    : IQueryHandler<GetCurrentUserQuery, ErrorOr<CurrentUserResponse>>
{
    public async Task<ErrorOr<CurrentUserResponse>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = currentUserService.User;
        var account = user.Email is null
            ? null
            : await userAccountService.FindByEmailAsync(user.Email, cancellationToken);

        ErrorOr<CurrentUserResponse> response = new CurrentUserResponse(
            user.Id,
            user.Email,
            user.Roles,
            account?.TwoFactorEnabled ?? false,
            account?.PhoneNumber,
            account?.PhoneNumberConfirmed ?? false,
            account?.PendingPhoneNumber);
        return response;
    }
}
