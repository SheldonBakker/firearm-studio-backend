using System.Security.Cryptography;
using System.Text;
using FirearmStudio.Application.Model.Options;
using Microsoft.Extensions.Primitives;

namespace FirearmStudio.WebApi.Middleware;

public sealed class ApiKeyMiddleware(ApiKeySettings settings) : IMiddleware
{
    private readonly byte[] _expected = Encoding.UTF8.GetBytes(settings.Key);

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
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

    // Accept if any supplied header value matches (a proxy may legitimately duplicate the header);
    // FixedTimeEquals keeps the comparison constant-time. Avoids comma-joining multiple values.
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
