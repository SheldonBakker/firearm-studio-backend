using Asp.Versioning;
using FirearmStudio.Application.Users;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class UsersController(IUserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(CancellationToken ct)
    {
        var result = await userManagementService.ListUsersAsync(ct);
        return result.IsError ? this.ToProblem(result) : Ok(result.Value);
    }

    [HttpPost("invite")]
    public async Task<ActionResult> Invite(InviteUserRequest request, CancellationToken ct)
    {
        var result = await userManagementService.InviteUserAsync(request, ct);
        return result.IsError ? this.ToProblem(result) : Created($"/api/v1/users/{result.Value.Id}", result.Value);
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<ActionResult> ChangeRole(Guid id, UpdateUserRoleRequest request, CancellationToken ct)
    {
        var result = await userManagementService.ChangeRoleAsync(id, request, ct);
        return result.IsError ? this.ToProblem(result) : Ok(result.Value);
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<ActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await userManagementService.DeactivateUserAsync(id, ct);
        return result.IsError ? this.ToProblem(result) : NoContent();
    }
}
