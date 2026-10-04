using FirearmStudio.Application.Model;
using FirearmStudio.Application.Products;
using FirearmStudio.Application.Products.CreateProduct;
using FirearmStudio.Application.Products.DeleteProduct;
using FirearmStudio.Application.Products.DeleteProductImage;
using FirearmStudio.Application.Products.GetProduct;
using FirearmStudio.Application.Products.GetProducts;
using FirearmStudio.Application.Products.UpdateProduct;
using FirearmStudio.Application.Products.UploadProductImage;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Common;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/products")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class ProductsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ProductResponse>>> List(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int? minStock = null,
        [FromQuery] int? maxStock = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetProductsQuery(
                pageNumber, pageSize, search, category, isActive,
                minPrice, maxPrice, minStock, maxStock, sortBy, sortDir),
            ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Get(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateProductCommand(request), ct);
        return result.IsError
            ? result.ToActionResult()
            : CreatedAtAction(nameof(Get), new { id = result.Value.Id, version = CurrentApiVersion }, result.Value);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateProductCommand(id, request), ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteProductCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/image")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ProductImageConstants.MaxImageBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProductImageConstants.MaxImageBytes)]
    public async Task<ActionResult<ProductResponse>> UploadImage(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(
            new UploadProductImageCommand(id, stream, file.ContentType, file.Length), ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}/image")]
    [Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
    public async Task<ActionResult> DeleteImage(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteProductImageCommand(id), ct);
        return result.ToActionResult();
    }
}
