using FirearmStudio.Domain.Services;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public sealed class IdNumberMaskTests
{
    [Theory]
    [InlineData("8001015009087", "800101****087")]
    [InlineData("1234567890", "123456*890")]
    [InlineData("AB123456789012", "AB1234*****012")]
    public void Mask_KeepsLeadingSixAndTrailingThree(string idNumber, string expected) =>
        Assert.Equal(expected, IdNumberMask.Mask(idNumber));

    [Theory]
    [InlineData("123456789")]
    [InlineData("12345")]
    [InlineData("")]
    public void Mask_HidesEverythingWhenTooShortToRevealSafely(string idNumber)
    {
        var masked = IdNumberMask.Mask(idNumber);

        Assert.Equal(new string('*', idNumber.Length), masked);
    }

    [Fact]
    public void Mask_PreservesLength()
    {
        const string IdNumber = "8001015009087";

        Assert.Equal(IdNumber.Length, IdNumberMask.Mask(IdNumber).Length);
    }
}
