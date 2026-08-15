using Asp.Versioning;
using FirearmStudio.Application.Auth;
using FirearmStudio.Application.Auth.AcceptInvite;
using FirearmStudio.Application.Auth.Login;
using FirearmStudio.Application.Auth.PasswordReset;
using FirearmStudio.Application.Auth.Register;
using FirearmStudio.Application.Auth.ResendCode;
using FirearmStudio.Application.Auth.Tokens;
using FirearmStudio.Application.Auth.VerifyEmail;
using FirearmStudio.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FirearmStudio.WebApi.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/auth")]
[AllowAnonymous]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new RegisterCommand(request), ct);

        return result.IsError
            ? result.ToActionResult()
            : Accepted(new { message = "If that address can be registered, a verification code is on its way." });
    }

    [HttpPost("verify-email")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new VerifyEmailCommand(request), ct);

        return result.IsError ? result.ToActionResult() : Ok(result.Value);
    }

    [HttpPost("resend-code")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> ResendCode(ResendCodeRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new ResendCodeCommand(request), ct);

        return result.IsError
            ? result.ToActionResult()
            : Accepted(new { message = "If that address can receive a code, one is on its way." });
    }

    [HttpPost("login")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new LoginCommand(request), ct);

        return result.IsError ? result.ToActionResult() : Ok(result.Value);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("public")]
    public async Task<ActionResult> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new RefreshCommand(request), ct);

        return result.IsError ? result.ToActionResult() : Ok(result.Value);
    }

    [HttpPost("logout")]
    [EnableRateLimiting("public")]
    public async Task<ActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new LogoutCommand(request), ct);

        return result.IsError ? result.ToActionResult() : NoContent();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new ForgotPasswordCommand(request), ct);

        return result.IsError ? result.ToActionResult() : NoContent();
    }

    [HttpPost("accept-invite")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> AcceptInvite(AcceptInviteRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new AcceptInviteCommand(request), ct);

        return result.IsError ? result.ToActionResult() : Ok(result.Value);
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new ResetPasswordCommand(request), ct);

        return result.IsError ? result.ToActionResult() : NoContent();
    }
}
