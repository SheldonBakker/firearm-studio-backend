using System.Security.Claims;
using System.Text;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Authentication;
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
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                $"Missing required configuration section '{JwtSettings.SectionName}'.");

        if (string.IsNullOrWhiteSpace(settings.SigningKey))
        {
            throw new InvalidOperationException(
                $"'{JwtSettings.SectionName}:SigningKey' is required. Generate one with " +
                "'openssl rand -base64 64'.");
        }

        services.AddSingleton(settings);

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(settings.SigningKey));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = settings.Audience,

                    ValidateLifetime = true,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,

                    ValidAlgorithms = settings.ValidAlgorithms,

                    NameClaimType = AppClaimTypes.Subject,
                    RoleClaimType = ClaimTypes.Role,

                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");
                        logger.LogWarning(
                            context.Exception,
                            "Token validation failed: {Message}",
                            context.Exception.Message);
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();

        return services;
    }
}
