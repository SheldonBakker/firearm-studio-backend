using FirearmStudio.Application.Accounting;
using FirearmStudio.Application.Accounting.GetAccountingConnection;
using FirearmStudio.Application.Accounting.RegisterAccountingConnection;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/accounting")]
[Authorize(Roles = AppRoles.Policy.AdminOnly)]
public sealed class AccountingController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("connections")]
    [ProducesResponseType(typeof(AccountingConnectionDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AccountingConnectionDetailsResponse?>> GetConnection(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAccountingConnectionQuery(), ct);
        return result.ToActionResult();
    }

    [HttpPost("register")]
    [EnableRateLimiting("accounting-register")]
    public async Task<ActionResult<AccountingConnectionResponse>> Register(
        RegisterAccountingConnectionRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new RegisterAccountingConnectionCommand(request), ct);
        return result.IsError
            ? result.ToActionResult()
            : Created(VersionedUrl("accounting/connections"), result.Value);
    }
}
