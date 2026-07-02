using System.Reflection;
using FirearmStudio.Application.Invoices.MonthlyInvoiceGeneration;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FirearmStudio.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddScoped<IMonthlyInvoiceGenerator, MonthlyInvoiceGenerator>();

        return services;
    }
}
