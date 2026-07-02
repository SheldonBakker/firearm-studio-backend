using Asp.Versioning;
using FirearmStudio.Application.Contact;
using FirearmStudio.Application.Contact.SubmitContactForm;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/contact")]
[AllowAnonymous]
public sealed class ContactController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Submit(ContactFormRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new SubmitContactFormCommand(request), ct);
        return result.ToActionResult();
    }
}
