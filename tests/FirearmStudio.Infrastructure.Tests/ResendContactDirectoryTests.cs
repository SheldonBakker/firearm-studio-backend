using System.Net;
using System.Text.Json;
using FirearmStudio.Application.Abstractions.Email;
using FirearmStudio.Infrastructure.Options;
using FirearmStudio.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class ResendContactDirectoryTests
{
    private sealed class CapturingHandler(
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{}"),
            };
        }
    }

    private static ResendSettings DefaultSettings() => new()
    {
        ApiKey = "test-api-key",
        BaseUrl = "https://api.resend.com",
        FromAddress = "no-reply@firearmstudio.com",
        ContactSegmentId = "segment-abc-123",
    };

    private static (ResendContactDirectory Directory, CapturingHandler Handler) Build(
        ResendSettings? settings = null,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        settings ??= DefaultSettings();
        var handler = new CapturingHandler(statusCode);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/"),
        };
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "Authorization", $"Bearer {settings.ApiKey}");
        return (new ResendContactDirectory(
            httpClient, settings, NullLogger<ResendContactDirectory>.Instance), handler);
    }

    private static JsonElement ParseBody(string? body) =>
        JsonDocument.Parse(body!).RootElement;

    [Fact]
    public async Task AddContact_posts_to_contacts_path()
    {
        var (dir, handler) = Build();

        await dir.AddContactAsync(
            new ContactEntry("user@example.com", "Jane", "Doe"),
            default);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("contacts", handler.LastRequest!.RequestUri!.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task AddContact_includes_email_and_names_in_body()
    {
        var (dir, handler) = Build();

        await dir.AddContactAsync(
            new ContactEntry("user@example.com", "Jane", "Doe"),
            default);

        var body = ParseBody(handler.LastRequestBody);
        Assert.Equal("user@example.com", body.GetProperty("email").GetString());
        Assert.Equal("Jane", body.GetProperty("first_name").GetString());
        Assert.Equal("Doe", body.GetProperty("last_name").GetString());
    }

    [Fact]
    public async Task AddContact_includes_segment_id_in_body()
    {
        var (dir, handler) = Build();

        await dir.AddContactAsync(
            new ContactEntry("user@example.com", null, null),
            default);

        var body = ParseBody(handler.LastRequestBody);
        var segments = body.GetProperty("segments");
        Assert.Equal(1, segments.GetArrayLength());
        Assert.Equal("segment-abc-123", segments[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task AddContact_includes_bearer_auth_header()
    {
        var (dir, handler) = Build();

        await dir.AddContactAsync(
            new ContactEntry("user@example.com", "A", "B"),
            default);

        Assert.True(handler.LastRequest!.Headers.TryGetValues("Authorization", out var values));
        Assert.Contains("Bearer test-api-key", values);
    }

    [Fact]
    public async Task AddContact_no_ops_when_segment_id_is_blank()
    {
        var settings = new ResendSettings
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.resend.com",
            FromAddress = "no-reply@firearmstudio.com",
            ContactSegmentId = "",
        };
        var (dir, handler) = Build(settings);

        await dir.AddContactAsync(
            new ContactEntry("user@example.com", "Jane", "Doe"),
            default);

        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task AddContact_throws_on_non_2xx_response()
    {
        var (dir, _) = Build(statusCode: HttpStatusCode.UnprocessableEntity);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            dir.AddContactAsync(
                new ContactEntry("user@example.com", "Jane", "Doe"),
                default));
    }
}
