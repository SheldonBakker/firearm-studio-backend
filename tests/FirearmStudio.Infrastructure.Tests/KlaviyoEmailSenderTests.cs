using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Services;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class KlaviyoEmailSenderTests
{
    private sealed class FakeKlaviyoClient : IKlaviyoClient
    {
        public string? Metric { get; private set; }
        public string? Email { get; private set; }
        public IReadOnlyDictionary<string, object?>? Properties { get; private set; }
        public bool SubscribeWasCalled { get; private set; }

        public Task TrackEventAsync(
            string metricName,
            string email,
            string? name,
            IReadOnlyDictionary<string, object?> properties,
            CancellationToken cancellationToken)
        {
            Metric = metricName;
            Email = email;
            Properties = properties;
            return Task.CompletedTask;
        }

        public Task SubscribeProfileAsync(
            string listId,
            string email,
            CancellationToken cancellationToken)
        {
            SubscribeWasCalled = true;
            return Task.CompletedTask;
        }
    }

    [Theory]
    [InlineData(OtpPurpose.EmailConfirmation, "Signup Verification Code")]
    [InlineData(OtpPurpose.PasswordReset, "Password Reset Code")]
    [InlineData(OtpPurpose.Invite, "Team Invite Code")]
    [InlineData(OtpPurpose.TwoFactor, "Login Verification Code")]
    [InlineData(OtpPurpose.PhoneChange, "Phone Verification Code")]
    public async Task Each_purpose_maps_to_its_own_metric(OtpPurpose purpose, string expected)
    {
        var klaviyo = new FakeKlaviyoClient();
        var sender = new KlaviyoEmailSender(klaviyo);

        await sender.SendOtpAsync("user@example.com", "User", purpose, "123456", 15, default);

        Assert.Equal(expected, klaviyo.Metric);
    }

    [Fact]
    public async Task Every_OtpPurpose_value_maps_to_a_non_empty_metric()
    {
        foreach (var purpose in Enum.GetValues<OtpPurpose>())
        {
            var klaviyo = new FakeKlaviyoClient();
            var sender = new KlaviyoEmailSender(klaviyo);

            await sender.SendOtpAsync("user@example.com", "User", purpose, "123456", 15, default);

            Assert.False(string.IsNullOrWhiteSpace(klaviyo.Metric));
        }
    }

    [Fact]
    public async Task Code_and_expiry_travel_as_event_properties()
    {
        var klaviyo = new FakeKlaviyoClient();
        var sender = new KlaviyoEmailSender(klaviyo);

        await sender.SendOtpAsync(
            "user@example.com", "User", OtpPurpose.EmailConfirmation, "123456", 15, default);

        Assert.NotNull(klaviyo.Properties);
        Assert.Equal("123456", klaviyo.Properties!["code"]);
        Assert.Equal(15, klaviyo.Properties["expires_in_minutes"]);
    }

    [Fact]
    public async Task Sending_a_code_never_subscribes_the_profile_to_a_list()
    {
        var klaviyo = new FakeKlaviyoClient();
        var sender = new KlaviyoEmailSender(klaviyo);

        await sender.SendOtpAsync(
            "user@example.com", null, OtpPurpose.PasswordReset, "654321", 15, default);

        Assert.False(klaviyo.SubscribeWasCalled);
    }
}
