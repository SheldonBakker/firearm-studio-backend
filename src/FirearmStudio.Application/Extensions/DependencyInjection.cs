using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FirearmStudio.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
