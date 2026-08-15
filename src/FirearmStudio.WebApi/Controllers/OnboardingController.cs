using Asp.Versioning;
using FirearmStudio.Application.Onboarding;
using FirearmStudio.Application.Onboarding.CreateCompanyOnboarding;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/onboarding")]
[Authorize]
public sealed class OnboardingController(IMediator mediator) : ControllerBase
{
    [HttpPost("company")]
    public async Task<ActionResult> CreateCompany(CreateCompanyRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateCompanyOnboardingCommand(request), ct);
        if (result.IsError)
        {
            return result.ToActionResult();
        }

        return Created("/api/v1/company", new
        {
            company = result.Value,
            message = "Company created and you are its admin. Call /auth/refresh to receive your " +
                      "company_id and admin role in a new access token.",
        });
    }
}
