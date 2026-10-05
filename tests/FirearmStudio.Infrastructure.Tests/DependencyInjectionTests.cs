using FirearmStudio.Application.Abstractions;
using FirearmStudio.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

[CollectionDefinition("DependencyInjection", DisableParallelization = true)]
public sealed class DependencyInjectionCollection { }

[Collection("DependencyInjection")]
public class DependencyInjectionTests
{
    private static IConfiguration BuildConfiguration(
        string? credentialProtectionKey,
        string provider = "s3",
        string? resendApiKey = "test-api-key",
        string? resendFromAddress = "no-reply@example.com")
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["ResendSettings:ApiKey"] = resendApiKey,
            ["ResendSettings:FromAddress"] = resendFromAddress,
            ["NotificationSettings:PublicBaseUrl"] = "https://api.example.test",
            ["FileStorageSettings:Provider"] = provider,
            ["FileStorageSettings:BucketName"] = "test-bucket",
            ["FileStorageSettings:ServiceUrl"] = "https://s3.example.test",
            ["FileStorageSettings:AccessKeyId"] = "test-access-key",
            ["FileStorageSettings:SecretAccessKey"] = "test-secret-key",
        };

        if (credentialProtectionKey is not null)
        {
            values["CredentialProtectionSettings:Key"] = credentialProtectionKey;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void AddInfrastructure_throws_when_the_file_storage_provider_is_not_s3()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddInfrastructure(BuildConfiguration(null, provider: "azure")));

        Assert.Contains("Provider", ex.Message);
    }

    [Fact]
    public void AddInfrastructure_throws_outside_Development_when_ResendSettings_ApiKey_is_missing()
    {
        var saved = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

            var ex = Assert.Throws<InvalidOperationException>(
                () => new ServiceCollection().AddInfrastructure(
                    BuildConfiguration(null, resendApiKey: "")));

            Assert.Contains("ResendSettings:ApiKey", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", saved);
        }
    }

    [Fact]
    public void AddInfrastructure_throws_outside_Development_when_ResendSettings_FromAddress_is_missing()
    {
        var saved = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

            var ex = Assert.Throws<InvalidOperationException>(
                () => new ServiceCollection().AddInfrastructure(
                    BuildConfiguration(null, resendFromAddress: "")));

            Assert.Contains("ResendSettings:FromAddress", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", saved);
        }
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

    [Fact]
    public void ITransactionalEmailSender_IContactDirectory_IOtpDispatcher_are_registered()
    {
        var services = new ServiceCollection().AddInfrastructure(BuildConfiguration(null));

        Assert.Contains(services, s => s.ServiceType == typeof(ITransactionalEmailSender));
        Assert.Contains(services, s => s.ServiceType == typeof(IContactDirectory));
        Assert.Contains(services, s => s.ServiceType == typeof(IOtpDispatcher));
    }
}
