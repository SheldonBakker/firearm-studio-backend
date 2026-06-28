using Asp.Versioning;
using FirearmStudio.Application.Companies;
using FirearmStudio.Application.Companies.GetCompany;
using FirearmStudio.Application.Companies.UpdateCompany;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/company")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class CompanyController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CompanyDetailsResponse>> Get(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCompanyQuery(), ct);
        return result.ToActionResult();
    }

    [HttpPatch]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult> Update(UpdateCompanyRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateCompanyCommand(request), ct);
        return result.ToActionResult();
    }
}
