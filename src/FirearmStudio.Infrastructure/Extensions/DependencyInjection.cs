using System.Net.Http.Headers;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Infrastructure.Identity;
using FirearmStudio.Infrastructure.Options;
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

        services.AddDbContext<AuthDbContext>(options =>
            options
                .UseNpgsql(dataSource, npgsql =>
                {
                    NpgsqlDataSourceFactory.MapAuthEnums(npgsql);
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

                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<AuthDbContext>();

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IUserAccountService, IdentityUserAccountService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IEmailSender, KlaviyoEmailSender>();
        services.AddScoped<IOtpDispatcher, OtpDispatcher>();

        AddCustomerEngagement(services, configuration);
        AddNotificationSettings(services, configuration);
        AddFileStorage(services, configuration);
        AddSageAccounting(services);

        return services;
    }

    private static void AddCustomerEngagement(IServiceCollection services, IConfiguration configuration)
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

        var engagementSettings = configuration
            .GetSection(CustomerEngagementSettings.SectionName)
            .Get<CustomerEngagementSettings>()
            ?? new CustomerEngagementSettings();

        services.AddSingleton(engagementSettings);

        services.AddHttpClient<ICustomerEngagementClient, KlaviyoClient>(client =>
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

    private static void AddFileStorage(IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(FileStorageSettings.SectionName).Get<FileStorageSettings>()
            ?? new FileStorageSettings();

        if (!string.Equals(settings.Provider, "s3", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported '{FileStorageSettings.SectionName}:Provider' value '{settings.Provider}'. Only 's3' is supported.");
        }

        var missing = string.IsNullOrWhiteSpace(settings.BucketName)
            || string.IsNullOrWhiteSpace(settings.ServiceUrl)
            || string.IsNullOrWhiteSpace(settings.AccessKeyId)
            || string.IsNullOrWhiteSpace(settings.SecretAccessKey);

        if (missing)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? string.Empty;

            if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Missing required '{FileStorageSettings.SectionName}' configuration " +
                    "(BucketName, ServiceUrl, AccessKeyId, SecretAccessKey). Set FileStorageSettings__* in .env or user-secrets.");
            }

            Console.Error.WriteLine(
                $"[WARNING] {FileStorageSettings.SectionName} is not fully configured. Product image storage will not function.");
        }

        services.AddSingleton(settings);
        services.AddSingleton<IFileStorage, S3FileStorage>();
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
        services.AddHttpClient<IAccountingConnectionValidator, SageAccountingClient>(client =>
        {
            client.Timeout = SageAccountingTimeout;
            client.BaseAddress = new Uri(SageAccountingBaseUrl + "/");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
    }
}
