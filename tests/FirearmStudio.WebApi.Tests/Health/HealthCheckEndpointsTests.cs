using System.Net;
using Xunit;

namespace FirearmStudio.WebApi.Tests.Health;

public sealed class HealthCheckEndpointsTests(HealthCheckFactory factory)
    : IClassFixture<HealthCheckFactory>
{
    [Fact]
    public async Task Readiness_returns_503_and_names_db_checks_when_database_is_unreachable()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("application-db", body);
        Assert.Contains("auth-db", body);
        Assert.Contains("Unhealthy", body);
    }

    [Fact]
    public async Task Readiness_body_leaks_no_connection_or_exception_detail()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("127.0.0.1", body);
        Assert.DoesNotContain("fs_test", body);
        Assert.DoesNotContain("Exception", body);
        Assert.DoesNotContain("Npgsql", body);
    }

    [Fact]
    public async Task Liveness_returns_200_when_database_is_unreachable()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
