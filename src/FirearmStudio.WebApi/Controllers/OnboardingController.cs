using Asp.Versioning;
using FirearmStudio.Application.Onboarding;
using FirearmStudio.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/onboarding")]
[Authorize]
public sealed class OnboardingController(IOnboardingService onboardingService) : ControllerBase
{
    [HttpPost("company")]
    public async Task<ActionResult> CreateCompany(CreateCompanyRequest request, CancellationToken ct)
    {
        var result = await onboardingService.CreateCompanyAsync(request, ct);
        if (result.IsError)
        {
            return this.ToProblem(result);
        }

        return Created($"/api/v1/companies/{result.Value.Id}", new
        {
            company = result.Value,
            message = "Company created and you are its admin. Refresh your Supabase session to " +
                      "receive your company_id and admin role in the access token.",
        });
    }
}
