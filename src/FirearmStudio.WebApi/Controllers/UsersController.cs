using FirearmStudio.Application.Model;
using FirearmStudio.Application.Users;
using FirearmStudio.Application.Users.ChangeUserRole;
using FirearmStudio.Application.Users.DeactivateUser;
using FirearmStudio.Application.Users.InviteUser;
using FirearmStudio.Application.Users.ListUsers;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/users")]
[Authorize(Roles = AppRoles.Policy.AdminOnly)]
public sealed class UsersController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AppUserResponse>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListUsersQuery(pageNumber, pageSize), ct);
        return result.ToActionResult();
    }

    [HttpPost("invite")]
    public async Task<ActionResult> Invite(InviteUserRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new InviteUserCommand(request), ct);
        return result.IsError ? result.ToActionResult() : Created(VersionedUrl("users"), result.Value);
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<ActionResult> ChangeRole(Guid id, UpdateUserRoleRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new ChangeUserRoleCommand(id, request), ct);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<ActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeactivateUserCommand(id), ct);
        return result.ToActionResult();
    }
}
