using FirearmStudio.Application.Contact;
using FirearmStudio.Application.Contact.SubmitContactForm;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FirearmStudio.WebApi.Controllers;

[Route("api/v{version:apiVersion}/contact")]
[AllowAnonymous]
public sealed class ContactController(IMediator mediator) : ApiControllerBase
{
    [HttpPost]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> Submit(ContactFormRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new SubmitContactFormCommand(request), ct);
        return result.ToActionResult();
    }
}
