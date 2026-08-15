using FirearmStudio.Application.Users;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class DataModelAdditionsTests
{
    [Fact]
    public void OtpPurpose_has_the_two_new_purposes_appended()
    {
        Assert.Equal(3, (int)OtpPurpose.TwoFactor);
        Assert.Equal(4, (int)OtpPurpose.PhoneChange);
        Assert.Equal(5, Enum.GetValues<OtpPurpose>().Length);
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
