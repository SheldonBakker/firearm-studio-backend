using Asp.Versioning;
using FirearmStudio.Application.StorageRecords;
using FirearmStudio.Application.StorageRecords.GetStorageRecords;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Application.StorageRecords.GetCustomerStorageRecords;
using FirearmStudio.Application.StorageRecords.ReleaseStorage;
using FirearmStudio.Application.StorageRecords.StartStorage;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}")]
[Authorize(Roles = AppRoles.Policy.AnyAuthenticatedRole)]
public sealed class StorageRecordsController(IMediator mediator) : ControllerBase
{
    [HttpGet("storage")]
    public async Task<ActionResult<IReadOnlyList<StorageRecordDto>>> GetAll(
        [FromQuery] StorageStatus? storageStatus,
        [FromQuery] string? serialNumber,
        [FromQuery] string? customerName,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetStorageRecordsQuery(storageStatus, serialNumber, customerName), ct);
        return result.ToActionResult();
    }

    [HttpGet("storage/customer/{customerId:guid}")]
    public async Task<ActionResult<IReadOnlyList<CustomerStorageRecordDto>>> ForCustomer(Guid customerId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCustomerStorageRecordsQuery(customerId), ct);
        return result.ToActionResult();
    }

    [HttpPost("firearms/{firearmId:guid}/storage")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Start(Guid firearmId, StartStorageRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new StartStorageCommand(firearmId, request), ct);
        return result.IsError
            ? result.ToActionResult()
            : Created($"/api/v1/storage/{result.Value}", new { Id = result.Value });
    }

    [HttpPatch("storage-records/{id:guid}/release")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Release(Guid id, ReleaseStorageRequest? request, CancellationToken ct)
    {
        var result = await mediator.Send(new ReleaseStorageCommand(id, request), ct);
        return result.ToActionResult();
    }
}
