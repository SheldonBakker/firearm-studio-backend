using FirearmStudio.Application.Auth;
using FirearmStudio.Application.Users;
using FirearmStudio.Application.Users.UpdatePhone;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class PhoneNumberValidationTests
{
    private static readonly UpdatePhoneRequestValidator UpdatePhone = new();
    private static readonly RegisterRequestValidator Register = new();

    [Theory]
    [InlineData("+27821234567")]
    [InlineData("+14155550123")]
    [InlineData("+441632960961")]
    [InlineData("+12345678")]
    [InlineData("+123456789012345")]
    public void Accepts_valid_e164_numbers(string phone)
    {
        var result = UpdatePhone.Validate(new UpdatePhoneRequest(phone));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("0821234567")]
    [InlineData("27821234567")]
    [InlineData("+0821234567")]
    [InlineData("+27 82 123 4567")]
    [InlineData("")]
    [InlineData("+")]
    [InlineData("+27abc4567")]
    [InlineData("+27821234567\n")]
    [InlineData("+27821234567\r\n")]
    [InlineData("+1234567")]
    [InlineData("+1234567890123456")]
    public void Rejects_non_e164_numbers(string phone)
    {
        var result = UpdatePhone.Validate(new UpdatePhoneRequest(phone));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Register_accepts_null_phone()
    {
        var result = Register.Validate(new RegisterRequest("user@example.com", "CorrectHorse123", null));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Register_rejects_malformed_phone_when_present()
    {
        var result = Register.Validate(new RegisterRequest("user@example.com", "CorrectHorse123", "0821234567"));
        Assert.False(result.IsValid);
    }
}
