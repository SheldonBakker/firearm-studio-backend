using FirearmStudio.Application.Extensions;
using FirearmStudio.Infrastructure.Extensions;
using FirearmStudio.WebApi.BackgroundJobs;
using FirearmStudio.WebApi.Extensions;
using FirearmStudio.WebApi.Extensions.Authentication;
using FirearmStudio.WebApi.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

try
{
    DotNetEnv.Env.TraversePath().Load();
}
catch (Exception)
{
    // No readable/parseable .env (e.g. in the container) — fall back to environment variables.
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks();

builder.Services
    .AddWebApi()
    .AddApiKey(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddWebAuthentication(builder.Configuration)
    .AddApplication();

builder.Services.AddHostedService<MonthlyInvoiceGenerationService>();

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
