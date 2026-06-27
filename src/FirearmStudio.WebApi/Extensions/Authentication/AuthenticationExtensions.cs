using System.Security.Claims;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FirearmStudio.WebApi.Extensions.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddWebAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(SupabaseJwtSettings.SectionName)
            .Get<SupabaseJwtSettings>()
            ?? throw new InvalidOperationException(
                $"Missing required configuration section '{SupabaseJwtSettings.SectionName}'.");

        services.AddSingleton(settings);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = settings.Authority;
                if (!string.IsNullOrWhiteSpace(settings.MetadataAddress))
                {
                    options.MetadataAddress = settings.MetadataAddress;
                }

                options.RequireHttpsMetadata = true;

                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = settings.Audience,

                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidAlgorithms = settings.ValidAlgorithms,

                    NameClaimType = SupabaseClaimTypes.Subject,
                    RoleClaimType = ClaimTypes.Role,

                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("SupabaseJwtBearer");
                        logger.LogWarning(
                            context.Exception,
                            "Supabase token validation failed: {Message}",
                            context.Exception.Message);
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddSingleton<IClaimsTransformation, SupabaseRolesClaimsTransformer>();

        services.AddAuthorization();

        return services;
    }
}
