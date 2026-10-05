using FirearmStudio.Application.Model;
using FirearmStudio.Application.Products;
using FirearmStudio.Application.Products.GetPublicProducts;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/public/companies/{companyId:guid}/products")]
[AllowAnonymous]
[EnableCors(CorsPolicies.PublicRead)]
[EnableRateLimiting(RateLimitPolicies.PublicCatalogue)]
public sealed class PublicProductsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet(Name = nameof(List))]
    [OutputCache(PolicyName = OutputCachePolicies.PublicProducts)]
    [ProducesResponseType(typeof(PaginatedResponse<PublicProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginatedResponse<PublicProductResponse>>> List(
        Guid companyId,
        [FromQuery] string? key,
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetPublicProductsQuery(companyId, key, pageNumber, pageSize, search, category, sortBy, sortDir),
            ct);
        return result.ToActionResult();
    }
}
