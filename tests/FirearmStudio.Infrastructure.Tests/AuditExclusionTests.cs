using FirearmStudio.Domain.Entities;
using FirearmStudio.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public class AuditExclusionTests
{
    private static readonly HashSet<(Type Entity, string Property)> ShadowProperties = [(typeof(Product), "SkuLower")];

    [Fact]
    public void Customer_id_numbers_are_excluded_from_audit_logs()
    {
        Assert.True(TenantAndAuditInterceptor.AuditExcludedProperties.TryGetValue(
            typeof(Customer), out var excluded));

        Assert.Contains(nameof(Customer.IdNumber), excluded);
    }

    [Fact]
    public void Every_excluded_property_still_exists_on_its_entity()
    {
        foreach (var (type, properties) in TenantAndAuditInterceptor.AuditExcludedProperties)
        {
            foreach (var property in properties)
            {
                if (ShadowProperties.Contains((type, property)))
                {
                    continue;
                }

                Assert.True(
                    type.GetProperty(property) is not null,
                    $"'{type.Name}.{property}' is excluded from audit logs but no longer exists.");
            }
        }
    }
}
