using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Me.GetAdminCheck;

public sealed class GetAdminCheckQueryHandler(ICurrentUserService currentUserService)
    : IQueryHandler<GetAdminCheckQuery, ErrorOr<AdminCheckResponse>>
{
    public Task<ErrorOr<AdminCheckResponse>> Handle(GetAdminCheckQuery query, CancellationToken cancellationToken)
    {
        ErrorOr<AdminCheckResponse> response = new AdminCheckResponse(true, currentUserService.User.Id);
        return Task.FromResult(response);
    }
}
