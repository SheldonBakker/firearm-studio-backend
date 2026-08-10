using Asp.Versioning;
using FirearmStudio.Application.Model;
using FirearmStudio.Application.StorageRecords;
using FirearmStudio.Application.StorageRecords.GetCustomerStorageRecords;
using FirearmStudio.Application.StorageRecords.GetStorageRecords;
using FirearmStudio.Application.StorageRecords.StartStorage;
using FirearmStudio.Application.StorageRecords.UpdateStorageRecord;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Enums;
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
    public async Task<ActionResult<PaginatedResponse<StorageRecordDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] StorageStatus? storageStatus = null,
        [FromQuery] string? serialNumber = null,
        [FromQuery] string? customerName = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetStorageRecordsQuery(pageNumber, pageSize, storageStatus, serialNumber, customerName), ct);
        return result.ToActionResult();
    }

    [HttpGet("storage/customer/{customerId:guid}")]
    public async Task<ActionResult<PaginatedResponse<CustomerStorageRecordDto>>> ForCustomer(
        Guid customerId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCustomerStorageRecordsQuery(customerId, pageNumber, pageSize), ct);
        return result.ToActionResult();
    }

    [HttpPost("firearms/{firearmId:guid}/storage")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Start(Guid firearmId, StartStorageRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new StartStorageCommand(firearmId, request), ct);
        return result.IsError
            ? result.ToActionResult()
            : Created("/api/v1/storage", new { Id = result.Value });
    }

    [HttpPatch("storage-records/{id:guid}")]
    [Authorize(Roles = AppRoles.Policy.StaffOrAbove)]
    public async Task<ActionResult> Update(Guid id, UpdateStorageRecordRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateStorageRecordCommand(id, request), ct);
        return result.ToActionResult();
    }
}
