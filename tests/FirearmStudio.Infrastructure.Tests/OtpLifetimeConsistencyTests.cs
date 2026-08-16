using FirearmStudio.Domain.Common;
using FirearmStudio.Infrastructure.Identity;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public class OtpLifetimeConsistencyTests
{
    [Fact]
    public void OtpService_ttl_matches_the_domain_constant()
    {
        Assert.Equal(OtpConstants.CodeLifetimeMinutes, (int)OtpService.Ttl.TotalMinutes);
    }
}
