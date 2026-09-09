using FirearmStudio.Application.Users;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class DataModelAdditionsTests
{
    [Fact]
    public void OtpPurpose_has_four_values_and_TwoFactor_is_last()
    {
        Assert.Equal(3, (int)OtpPurpose.TwoFactor);
        Assert.Equal(4, Enum.GetValues<OtpPurpose>().Length);
    }

    [Fact]
    public void AppUserResponse_FromEntity_copies_the_phone_number()
    {
        var user = new AppUser
        {
            CompanyId = Guid.NewGuid(),
            Email = "user@example.com",
            PhoneNumber = "+27821234567",
            IsActive = true,
        };

        var response = AppUserResponse.FromEntity(user);

        Assert.Equal("+27821234567", response.PhoneNumber);
    }
}
