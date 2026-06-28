using Asp.Versioning;
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

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppUserResponse>>> List(CancellationToken ct)
    {
        var result = await mediator.Send(new ListUsersQuery(), ct);
        return result.ToActionResult();
    }

    [HttpPost("invite")]
    public async Task<ActionResult> Invite(InviteUserRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new InviteUserCommand(request), ct);
        return result.IsError ? result.ToActionResult() : Created($"/api/v1/users/{result.Value.Id}", result.Value);
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
