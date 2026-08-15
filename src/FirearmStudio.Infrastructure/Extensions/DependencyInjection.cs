using System.Net.Http.Headers;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Infrastructure.Identity;
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

        var dataSource = NpgsqlDataSourceFactory.Build(connectionString);

        services.AddScoped<TenantAndAuditInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            options
                .UseNpgsql(dataSource, NpgsqlDataSourceFactory.MapEnums)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<TenantAndAuditInterceptor>()));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Auth state, same database and data source, different schema. Deliberately without
        // TenantAndAuditInterceptor: identity records are not tenant-scoped.
        services.AddDbContext<AuthDbContext>(options =>
            options
                .UseNpgsql(dataSource, npgsql =>
                {
                    NpgsqlDataSourceFactory.MapAuthEnums(npgsql);

                    // Its own history table, so the two contexts cannot misread each
                    // other's applied migrations.
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
                })
                .UseSnakeCaseNamingConvention());

        services
            .AddIdentityCore<AppIdentityUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;

                // Confirmation is enforced at the login endpoint, using our own one-time
                // codes rather than Identity's token providers.
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<AuthDbContext>();

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IUserAccountService, IdentityUserAccountService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ITokenService, TokenService>();

        // Capability-named seam. Klaviyo is one adapter behind it, not the interface.
        services.AddScoped<IEmailSender, KlaviyoEmailSender>();

        AddKlaviyo(services, configuration);
        AddWhatsApp(services, configuration);
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

    private static void AddWhatsApp(IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(WahaSettings.SectionName).Get<WahaSettings>()
            ?? new WahaSettings();

        services.AddSingleton(settings);

        var complete = settings.Enabled
            && !string.IsNullOrWhiteSpace(settings.BaseUrl)
            && !string.IsNullOrWhiteSpace(settings.SessionId)
            && !string.IsNullOrWhiteSpace(settings.ApiKey)
            && Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _);

        if (settings.Enabled && !complete)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? string.Empty;

            if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"'{WahaSettings.SectionName}' is enabled but incomplete. Set " +
                    "WahaSettings__BaseUrl, WahaSettings__SessionId and WahaSettings__ApiKey " +
                    "(e.g. in .env or user-secrets).");
            }

            Console.Error.WriteLine(
                $"[WARNING] {WahaSettings.SectionName} is enabled but incomplete. " +
                "WhatsApp OTP delivery will be disabled.");
        }

        if (!complete)
        {
            // Dev/CI and any misconfigured-but-non-prod case: no-op adapter.
            services.AddSingleton<IWhatsAppSender, NullWhatsAppSender>();
            return;
        }

        services.AddHttpClient<IWhatsAppSender, WahaWhatsAppSender>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", settings.ApiKey);
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

        services.AddSingleton<ICredentialProtector, AesGcmCredentialProtector>();
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
