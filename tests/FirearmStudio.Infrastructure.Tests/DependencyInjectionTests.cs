using FirearmStudio.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public class DependencyInjectionTests
{
    private static IConfiguration BuildConfiguration(string? credentialProtectionKey)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["KlaviyoSettings:ApiKey"] = "test-api-key",
            ["NotificationSettings:PublicBaseUrl"] = "https://api.example.test",
        };

        if (credentialProtectionKey is not null)
        {
            values["CredentialProtectionSettings:Key"] = credentialProtectionKey;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void AddInfrastructure_throws_at_startup_when_credential_protection_key_is_missing()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddInfrastructure(BuildConfiguration(null)));

        Assert.Contains("CredentialProtectionSettings:Key", ex.Message);
    }

    [Fact]
    public void AddInfrastructure_throws_at_startup_when_credential_protection_key_is_not_base64()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddInfrastructure(BuildConfiguration("not-base64!")));

        Assert.Contains("CredentialProtectionSettings:Key", ex.Message);
    }

    [Fact]
    public void AddInfrastructure_succeeds_when_credential_protection_key_is_valid()
    {
        var key = Convert.ToBase64String(new byte[32]);

        var services = new ServiceCollection().AddInfrastructure(BuildConfiguration(key));

        Assert.Contains(services, s => s.ServiceType == typeof(FirearmStudio.Application.Abstractions.ICredentialProtector));
    }
}
