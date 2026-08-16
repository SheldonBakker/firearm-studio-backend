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
        if (IsNonApiPath(context))
        {
            await next(context);
            return;
        }

        if (AllowsAnonymous(context))
        {
            await next(context);
            return;
        }

        if (!IsValid(context.Request.Headers[settings.HeaderName]))
        {
            await Results.Problem(
                detail: "Missing or invalid API key.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: new Dictionary<string, object?> { ["code"] = "api_key.invalid" }).ExecuteAsync(context);
            return;
        }

        await next(context);
    }

    private static bool IsNonApiPath(HttpContext context)
        => !context.Request.Path.StartsWithSegments("/api");

    private static bool AllowsAnonymous(HttpContext context)
        => context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

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
