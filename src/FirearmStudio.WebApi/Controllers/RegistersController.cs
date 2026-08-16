using FirearmStudio.Application.Registers;
using FirearmStudio.Application.Registers.ExportStorageRegister;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/registers")]
[Authorize(Roles = AppRoles.Policy.ManagerOrAbove)]
public sealed class RegistersController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("firearms/export")]
    public Task<ActionResult> ExportFirearmsRegister(
        [FromQuery][BindRequired] DateOnly from,
        [FromQuery][BindRequired] DateOnly to,
        [FromQuery][BindRequired] RegisterExportFormat format,
        CancellationToken ct) =>
        Export(RegisterKind.Firearms, from, to, format, ct);

    [HttpGet("safe-custody/export")]
    public Task<ActionResult> ExportSafeCustodyRegister(
        [FromQuery][BindRequired] DateOnly from,
        [FromQuery][BindRequired] DateOnly to,
        [FromQuery][BindRequired] RegisterExportFormat format,
        CancellationToken ct) =>
        Export(RegisterKind.SafeCustody, from, to, format, ct);

    private async Task<ActionResult> Export(
        RegisterKind kind, DateOnly from, DateOnly to, RegisterExportFormat format, CancellationToken ct)
    {
        var result = await mediator.Send(new ExportStorageRegisterQuery(kind, from, to, format), ct);
        return result.IsError
            ? result.ToActionResult()
            : File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }
}
