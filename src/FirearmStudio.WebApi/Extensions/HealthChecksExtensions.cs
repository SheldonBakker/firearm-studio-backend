using System.Text.Json;
using FirearmStudio.Infrastructure.Identity;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FirearmStudio.WebApi.Extensions;

public static class HealthChecksExtensions
{
    private const string ReadinessTag = "readiness";
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(5);

    internal static class CheckNames
    {
        internal const string ApplicationDb = "application-db";
        internal const string AuthDb = "auth-db";
    }

    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(CheckNames.ApplicationDb, tags: [ReadinessTag])
            .AddDbContextCheck<AuthDbContext>(CheckNames.AuthDb, tags: [ReadinessTag]);

        services.Configure<HealthCheckServiceOptions>(opts =>
        {
            foreach (var reg in opts.Registrations.Where(r => r.Tags.Contains(ReadinessTag)))
            {
                reg.Timeout = CheckTimeout;
            }
        });

        return services;
    }

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadinessTag),
            ResponseWriter = WriteJsonResponse,
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
        });

        return app;
    }

    private static async Task WriteJsonResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        await using var writer = new Utf8JsonWriter(context.Response.Body);

        writer.WriteStartObject();
        writer.WriteString("status", report.Status.ToString());
        writer.WriteStartObject("checks");

        foreach (var (name, entry) in report.Entries)
        {
            writer.WriteString(name, entry.Status.ToString());
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
