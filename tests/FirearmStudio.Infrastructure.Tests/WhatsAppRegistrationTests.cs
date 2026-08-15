using FirearmStudio.Application.Abstractions;
using FirearmStudio.Infrastructure.Extensions;
using FirearmStudio.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class WhatsAppRegistrationTests
{
    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["KlaviyoSettings:ApiKey"] = "test-api-key",
            ["NotificationSettings:PublicBaseUrl"] = "https://api.example.test",
            ["CredentialProtectionSettings:Key"] = "0123456789abcdef0123456789abcdef",
        }).Build();

    [Fact]
    public void Registers_the_null_whatsapp_sender_when_waha_is_disabled()
    {
        var services = new ServiceCollection().AddInfrastructure(Config());

        Assert.Contains(services, s =>
            s.ServiceType == typeof(IWhatsAppSender) &&
            s.ImplementationType == typeof(NullWhatsAppSender));
    }
}
