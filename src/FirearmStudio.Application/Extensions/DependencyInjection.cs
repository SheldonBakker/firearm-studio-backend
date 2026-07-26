using System.Reflection;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Bookings;
using FirearmStudio.Application.Invoices.MonthlyInvoiceGeneration;
using FirearmStudio.Application.Licences.Reminders;
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
        services.AddScoped<IBookingRequestedOutbox, BookingRequestedOutbox>();
        services.AddScoped<IBookingRequestedDispatcher, BookingRequestedDispatcher>();
        services.AddScoped<ILicenceRenewalReminderDispatcher, LicenceRenewalReminderDispatcher>();
        services.AddScoped<ILicenceReminderGenerator, LicenceReminderGenerator>();

        return services;
    }
}
