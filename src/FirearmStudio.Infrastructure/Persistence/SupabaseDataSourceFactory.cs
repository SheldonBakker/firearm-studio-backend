using FirearmStudio.Domain.Enums;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace FirearmStudio.Infrastructure.Persistence;

public static class SupabaseDataSourceFactory
{
    public static NpgsqlDataSource Build(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);

        builder.MapEnum<CustomerType>("customer_type");
        builder.MapEnum<FirearmStatus>("firearm_status");
        builder.MapEnum<LicenceStatus>("licence_status");
        builder.MapEnum<StorageStatus>("storage_status");
        builder.MapEnum<InvoiceStatus>("invoice_status");
        builder.MapEnum<PaymentMethod>("payment_method");
        builder.MapEnum<AppRole>("app_role");

        return builder.Build();
    }

    public static void MapEnums(NpgsqlDbContextOptionsBuilder options)
    {
        options.MapEnum<CustomerType>("customer_type");
        options.MapEnum<FirearmStatus>("firearm_status");
        options.MapEnum<LicenceStatus>("licence_status");
        options.MapEnum<StorageStatus>("storage_status");
        options.MapEnum<InvoiceStatus>("invoice_status");
        options.MapEnum<PaymentMethod>("payment_method");
        options.MapEnum<AppRole>("app_role");
    }
}
