using FirearmStudio.Application.Onboarding;
using FirearmStudio.Application.Onboarding.CreateCompanyOnboarding;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/onboarding")]
[Authorize]
public sealed class OnboardingController(IMediator mediator) : ApiControllerBase
{
    [HttpPost("company")]
    public async Task<ActionResult> CreateCompany(CreateCompanyRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateCompanyOnboardingCommand(request), ct);
        if (result.IsError)
        {
            return result.ToActionResult();
        }

        return Created(VersionedUrl("company"), new CreateCompanyOnboardingResponse(result.Value));
    }
}
