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
    private const string SageAccountingBaseUrl = "https://accounting.sageone.co.za/api/2.0.0";
    private static readonly TimeSpan SageAccountingTimeout = TimeSpan.FromSeconds(10);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        AddCredentialProtection(services, configuration);
        services.AddSingleton<IRegisterPdfRenderer, PdfSharpRegisterRenderer>();

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
        AddNotificationSettings(services, configuration);
        AddSageAccounting(services);

        return services;
    }

    private static void AddKlaviyo(IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(KlaviyoSettings.SectionName).Get<KlaviyoSettings>()
            ?? new KlaviyoSettings();

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? string.Empty;

            if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Missing required configuration '{KlaviyoSettings.SectionName}:ApiKey'. " +
                    "Set it via KlaviyoSettings__ApiKey in .env or user-secrets.");
            }

            Console.Error.WriteLine(
                $"[WARNING] {KlaviyoSettings.SectionName}:ApiKey is not configured. " +
                "Klaviyo integration will not function. Set KlaviyoSettings__ApiKey in .env or user-secrets.");
        }

        services.AddSingleton(settings);

        services.AddHttpClient<IKlaviyoClient, KlaviyoClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Klaviyo-API-Key {settings.ApiKey}");
            client.DefaultRequestHeaders.TryAddWithoutValidation("revision", settings.ApiRevision);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
    }

    private static void AddNotificationSettings(IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(NotificationSettings.SectionName).Get<NotificationSettings>()
            ?? new NotificationSettings();

        var isAbsoluteUri = Uri.TryCreate(settings.PublicBaseUrl, UriKind.Absolute, out _);

        if (string.IsNullOrWhiteSpace(settings.PublicBaseUrl) || !isAbsoluteUri)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? string.Empty;

            if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Missing or invalid required configuration '{NotificationSettings.SectionName}:PublicBaseUrl'. " +
                    "Set it to an absolute URI via NotificationSettings__PublicBaseUrl in .env or user-secrets.");
            }

            Console.Error.WriteLine(
                $"[WARNING] {NotificationSettings.SectionName}:PublicBaseUrl is not configured. " +
                "Booking calendar links will not function. Set NotificationSettings__PublicBaseUrl in .env or user-secrets.");
        }

        services.AddSingleton(settings);
    }

    private static void AddCredentialProtection(IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(CredentialProtectionSettings.SectionName)
            .Get<CredentialProtectionSettings>()
            ?? new CredentialProtectionSettings();

        services.AddSingleton(settings);

        services.AddSingleton<ICredentialProtector>(new AesGcmCredentialProtector(settings));
    }

    private static void AddSageAccounting(IServiceCollection services)
    {
        services.AddHttpClient<ISageAccountingClient, SageAccountingClient>(client =>
        {
            client.Timeout = SageAccountingTimeout;
            client.BaseAddress = new Uri(SageAccountingBaseUrl + "/");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
    }
}
