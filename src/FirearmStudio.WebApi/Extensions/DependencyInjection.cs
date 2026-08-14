using Asp.Versioning;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.WebApi.Common;
using Microsoft.OpenApi.Models;

namespace FirearmStudio.WebApi.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddWebApi(this IServiceCollection services)
    {
        services.AddProblemDetails();

        services
            .AddControllers(options => options.Filters.Add<ValidationFilter>());
        services.AddEndpointsApiExplorer();

        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("X-Api-Version"));
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Firearm Studio API",
                Version = "v1",
            });

            options.SchemaFilter<OptionalSchemaFilter>();

            const string bearerScheme = "Bearer";
            options.AddSecurityDefinition(bearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste an access token from POST /api/v1/auth/login (without the 'Bearer ' prefix).",
            });

            const string apiKeyScheme = "ApiKey";
            options.AddSecurityDefinition(apiKeyScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = ApiKeySettings.DefaultHeaderName,
                Description = "Shared API key required on all /api endpoints.",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = bearerScheme,
                        },
                    },
                    []
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = apiKeyScheme,
                        },
                    },
                    []
                },
            });
        });

        return services;
    }
}
