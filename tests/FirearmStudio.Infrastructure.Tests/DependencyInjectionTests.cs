using FirearmStudio.Application.Abstractions;
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
    public void AddInfrastructure_starts_up_when_the_credential_protection_key_is_missing()
    {
        var services = new ServiceCollection().AddInfrastructure(BuildConfiguration(null));

        Assert.Contains(services, s => s.ServiceType == typeof(ICredentialProtector));
    }

    [Fact]
    public void Resolving_the_protector_without_a_key_throws_naming_the_setting()
    {
        var provider = new ServiceCollection()
            .AddInfrastructure(BuildConfiguration(null))
            .BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<ICredentialProtector>());

        Assert.Contains("CredentialProtectionSettings:Key", ex.Message);
    }

    [Fact]
    public void Resolving_the_protector_with_a_non_base64_key_throws_naming_the_setting()
    {
        var provider = new ServiceCollection()
            .AddInfrastructure(BuildConfiguration("not-base64!"))
            .BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<ICredentialProtector>());

        Assert.Contains("CredentialProtectionSettings:Key", ex.Message);
    }

    [Fact]
    public void Resolving_the_protector_succeeds_when_the_key_is_valid()
    {
        var provider = new ServiceCollection()
            .AddInfrastructure(BuildConfiguration(Convert.ToBase64String(new byte[32])))
            .BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ICredentialProtector>());
    }
}
