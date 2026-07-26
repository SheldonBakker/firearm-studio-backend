using System.Security.Cryptography;
using System.Text;
using FirearmStudio.Application.Model.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Primitives;

namespace FirearmStudio.WebApi.Middleware;

public sealed class ApiKeyMiddleware(ApiKeySettings settings) : IMiddleware
{
    private readonly byte[] _expected = Encoding.UTF8.GetBytes(settings.Key);

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Pass through non-API paths (health, swagger, etc.)
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        // Pass through endpoints marked [AllowAnonymous]
        var ep = context.GetEndpoint();
        if (ep?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next(context);
            return;
        }

        if (!IsValid(context.Request.Headers[settings.HeaderName]))
        {
            await Results.Problem(
                detail: "Missing or invalid API key.",
                statusCode: StatusCodes.Status401Unauthorized).ExecuteAsync(context);
            return;
        }

        await next(context);
    }

    private bool IsValid(StringValues provided)
    {
        foreach (var value in provided)
        {
            if (value is not null
                && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(value), _expected))
            {
                return true;
            }
        }

        return false;
    }
}
