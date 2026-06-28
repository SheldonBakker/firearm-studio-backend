using FirearmStudio.Application.Model.Options;
using FirearmStudio.WebApi.Middleware;

namespace FirearmStudio.WebApi.Extensions;

public static class ApiKeyExtensions
{
    public static IServiceCollection AddApiKey(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(ApiKeySettings.SectionName).Get<ApiKeySettings>();
        if (settings is null || string.IsNullOrWhiteSpace(settings.Key))
        {
            throw new InvalidOperationException(
                $"Missing required configuration '{ApiKeySettings.SectionName}:Key' " +
                "(set ApiKeySettings__Key in .env or the environment).");
        }

        services.AddSingleton(settings);
        services.AddSingleton<ApiKeyMiddleware>();

        return services;
    }
}
