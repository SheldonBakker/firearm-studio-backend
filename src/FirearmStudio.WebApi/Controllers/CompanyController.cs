using FirearmStudio.Application.Companies;
using FirearmStudio.Application.Companies.GetCompany;
using FirearmStudio.Application.Companies.GetStorefrontAccess;
using FirearmStudio.Application.Companies.RevokeStorefrontKey;
using FirearmStudio.Application.Companies.RotateStorefrontKey;
using FirearmStudio.Application.Companies.UpdateCompany;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/company")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class CompanyController(IMediator mediator, IOutputCacheStore outputCache) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CompanyDetailsResponse>> Get(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCompanyQuery(), ct);
        return result.ToActionResult();
    }

    [HttpPatch]
    [Authorize(Roles = AppRoles.Policy.AdminOnly)]
    public async Task<ActionResult> Update(UpdateCompanyRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateCompanyCommand(request), ct);
        return result.ToActionResult();
    }

    [HttpGet("storefront")]
    [Authorize(Roles = AppRoles.Policy.AdminOnly)]
    [ProducesResponseType(typeof(StorefrontAccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorefrontAccessResponse>> GetStorefront(CancellationToken ct)
    {
        var result = await mediator.Send(new GetStorefrontAccessQuery(), ct);
        return result.IsError ? result.ToActionResult() : ToResponse(result.Value);
    }

    [HttpPost("storefront/key")]
    [Authorize(Roles = AppRoles.Policy.AdminOnly)]
    [ProducesResponseType(typeof(StorefrontAccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorefrontAccessResponse>> RotateStorefrontKey(CancellationToken ct)
    {
        var result = await mediator.Send(new RotateStorefrontKeyCommand(), ct);
        if (result.IsError)
        {
            return result.ToActionResult();
        }

        await EvictPublicProductsAsync(ct);
        return ToResponse(result.Value);
    }

    [HttpDelete("storefront/key")]
    [Authorize(Roles = AppRoles.Policy.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RevokeStorefrontKey(CancellationToken ct)
    {
        var result = await mediator.Send(new RevokeStorefrontKeyCommand(), ct);
        if (result.IsError)
        {
            return result.ToActionResult();
        }

        await EvictPublicProductsAsync(ct);
        return result.ToActionResult();
    }

    private Task EvictPublicProductsAsync(CancellationToken ct) =>
        outputCache.EvictByTagAsync(OutputCachePolicies.PublicProductsTag, ct).AsTask();

    private StorefrontAccessResponse ToResponse(StorefrontKeyResult result)
    {
        if (result.Key is null)
        {
            return new StorefrontAccessResponse(null, null);
        }

        var url = Url.ActionLink(
            nameof(PublicProductsController.List),
            "PublicProducts",
            new { version = CurrentApiVersion, companyId = result.CompanyId, key = result.Key });

        return new StorefrontAccessResponse(result.Key, url);
    }
}
