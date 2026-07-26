using System.Threading.RateLimiting;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Infrastructure.Extensions;
using FirearmStudio.WebApi.BackgroundJobs;
using FirearmStudio.WebApi.Extensions;
using FirearmStudio.WebApi.Extensions.Authentication;
using FirearmStudio.WebApi.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

try
{
    DotNetEnv.Env.TraversePath().Load();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to load .env file: {ex.Message}");
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

var fhSettings = builder.Configuration
    .GetSection(ForwardedHeadersSettings.SectionName)
    .Get<ForwardedHeadersSettings>() ?? new ForwardedHeadersSettings();

// Parse eagerly so a bad CIDR or IP fails at startup, not on the first request.
var knownNetworks = fhSettings.KnownNetworks
    .Select(cidr => System.Net.IPNetwork.Parse(cidr))
    .ToList();
var knownProxies = fhSettings.KnownProxies
    .Select(ip => System.Net.IPAddress.Parse(ip))
    .ToList();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = fhSettings.ForwardLimit;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    foreach (var network in knownNetworks)
    {
        options.KnownIPNetworks.Add(network);
    }

    foreach (var proxy in knownProxies)
    {
        options.KnownProxies.Add(proxy);
    }
});

builder.Services.AddHealthChecks();

builder.Services
    .AddWebApi()
    .AddApiKey(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddWebAuthentication(builder.Configuration)
    .AddApplication();

builder.Services.AddHostedService<MonthlyInvoiceGenerationService>();
builder.Services.AddHostedService<OutboxProcessorService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
            }));

    options.AddPolicy("public", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
            }));

    options.AddPolicy("public-write", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
            }));

    options.AddPolicy("sage-register", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(SupabaseClaimTypes.CompanyId)?.Value
            ?? context.User.FindFirst(SupabaseClaimTypes.Subject)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
            }));
});

// Only GET requests returning 200 OK are cached (framework default).
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("public-options", policy =>
        policy
            .Expire(TimeSpan.FromSeconds(60))
            .SetVaryByRouteValue("companyId"));

    options.AddPolicy("public-day", policy =>
        policy
            .Expire(TimeSpan.FromSeconds(20))
            .SetVaryByRouteValue("companyId", "rangeId")
            .SetVaryByQuery("packageId", "date"));

    options.AddPolicy("public-month", policy =>
        policy
            .Expire(TimeSpan.FromSeconds(60))
            .SetVaryByRouteValue("companyId", "rangeId")
            .SetVaryByQuery("year", "month", "packageId"));
});

var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
if (startupLogger.IsEnabled(LogLevel.Information))
{
    startupLogger.LogInformation(
        "ForwardedHeaders: {NetworkCount} known network(s), {ProxyCount} known proxy/proxies",
        fhSettings.KnownNetworks.Count,
        fhSettings.KnownProxies.Count);
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

app.UseRouting();

app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseOutputCache();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
