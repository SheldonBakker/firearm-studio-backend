using Asp.Versioning;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/me")]
[Authorize]
public sealed class MeController(ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public ActionResult<CurrentUserResponse> Get()
    {
        var user = currentUserService.User;
        return Ok(new CurrentUserResponse(user.Id, user.Email, user.Roles));
    }

    [HttpGet("admin-check")]
    [Authorize(Roles = AppRoles.Admin)]
    public ActionResult<AdminCheckResponse> AdminCheck()
    {
        return Ok(new AdminCheckResponse(true, currentUserService.User.Id));
    }

    public sealed record CurrentUserResponse(Guid Id, string? Email, IReadOnlyList<string> Roles);

    public sealed record AdminCheckResponse(bool IsAdmin, Guid Id);
}
