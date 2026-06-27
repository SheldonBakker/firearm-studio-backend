using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Application.Invoices;
using FirearmStudio.Application.Onboarding;
using FirearmStudio.Application.Users;
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

        var dbSettings = configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>()
            ?? throw new InvalidOperationException(
                $"Missing required configuration section '{DatabaseSettings.SectionName}'.");

        var dataSource = SupabaseDataSourceFactory.Build(dbSettings.ConnectionString);

        services.AddScoped<TenantAndAuditInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            options
                .UseNpgsql(dataSource, SupabaseDataSourceFactory.MapEnums)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<TenantAndAuditInterceptor>()));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IInvoiceGenerationService, InvoiceGenerationService>();

        return services;
    }
}
