using System.Net.Http.Headers;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Infrastructure.Persistence;
using FirearmStudio.Infrastructure.Persistence.Interceptors;
using FirearmStudio.Infrastructure.Services;
using FirearmStudio.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FirearmStudio.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No database connection string found. Set ConnectionStrings:DefaultConnection " +
                "(e.g. ConnectionStrings__DefaultConnection in .env or user-secrets).");
        }

        var dataSource = SupabaseDataSourceFactory.Build(connectionString);

        services.AddScoped<TenantAndAuditInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            options
                .UseNpgsql(dataSource, SupabaseDataSourceFactory.MapEnums)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<TenantAndAuditInterceptor>()));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        AddKlaviyo(services, configuration);

        return services;
    }

    private static void AddKlaviyo(IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(KlaviyoSettings.SectionName).Get<KlaviyoSettings>()
            ?? new KlaviyoSettings();

        services.AddSingleton(settings);

        services.AddHttpClient<IKlaviyoClient, KlaviyoClient>(client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Klaviyo-API-Key {settings.ApiKey}");
            client.DefaultRequestHeaders.TryAddWithoutValidation("revision", settings.ApiRevision);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
    }
}
