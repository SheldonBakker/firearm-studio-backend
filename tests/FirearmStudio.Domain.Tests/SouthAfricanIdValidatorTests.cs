using FirearmStudio.Domain.Services;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class SouthAfricanIdValidatorTests
{
    [Theory]
    [InlineData("8001015009087")]
    [InlineData("9202204720083")]
    [InlineData("0001015005083")]
    public void IsValid_accepts_a_13_digit_id_with_a_correct_luhn_checksum(string idNumber)
    {
        Assert.True(SouthAfricanIdValidator.IsValid(idNumber));
    }

    [Theory]
    [InlineData("8001015009080")]
    [InlineData("8001015009088")]
    [InlineData("9202204720080")]
    public void IsValid_rejects_a_13_digit_id_with_an_incorrect_luhn_checksum(string idNumber)
    {
        Assert.False(SouthAfricanIdValidator.IsValid(idNumber));
    }

    [Theory]
    [InlineData("A1234567")]
    [InlineData("PA0123456")]
    [InlineData("123456789012A")]
    public void IsValid_accepts_a_non_numeric_value_as_a_passport(string passportNumber)
    {
        Assert.True(SouthAfricanIdValidator.IsValid(passportNumber));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456789012345678901")]
    public void IsValid_rejects_values_outside_the_length_bounds(string idNumber)
    {
        Assert.False(SouthAfricanIdValidator.IsValid(idNumber));
    }

    [Fact]
    public void IsValid_accepts_a_passport_number_at_the_maximum_length()
    {
        Assert.True(SouthAfricanIdValidator.IsValid(new string('A', 20)));
    }

    [Fact]
    public void IsValid_rejects_null()
    {
        Assert.False(SouthAfricanIdValidator.IsValid(null!));
    }
}
