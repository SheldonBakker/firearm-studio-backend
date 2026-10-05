using FirearmStudio.WebApi.Common;
using FirearmStudio.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace FirearmStudio.WebApi.Tests;

public sealed class PublicProductsControllerAttributesTests
{
    private static readonly Type ControllerType = typeof(PublicProductsController);

    [Fact]
    public void Controller_carries_AllowAnonymous()
    {
        Assert.NotNull(ControllerType.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false)
            .Cast<AllowAnonymousAttribute>()
            .FirstOrDefault());
    }

    [Fact]
    public void Controller_carries_EnableCors_with_PublicRead_policy()
    {
        var attr = ControllerType.GetCustomAttributes(typeof(EnableCorsAttribute), inherit: false)
            .Cast<EnableCorsAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
        Assert.Equal(CorsPolicies.PublicRead, attr.PolicyName);
    }

    [Fact]
    public void Controller_carries_EnableRateLimiting_with_PublicCatalogue_policy()
    {
        var attr = ControllerType.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
            .Cast<EnableRateLimitingAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
        Assert.Equal(RateLimitPolicies.PublicCatalogue, attr.PolicyName);
    }

    [Fact]
    public void List_action_carries_OutputCache_with_PublicProducts_policy()
    {
        var method = ControllerType.GetMethod(nameof(PublicProductsController.List));
        Assert.NotNull(method);

        var attr = method.GetCustomAttributes(typeof(OutputCacheAttribute), inherit: false)
            .Cast<OutputCacheAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
        Assert.Equal(OutputCachePolicies.PublicProducts, attr.PolicyName);
    }

    [Fact]
    public void List_action_carries_HttpGet()
    {
        var method = ControllerType.GetMethod(nameof(PublicProductsController.List));
        Assert.NotNull(method);

        Assert.NotEmpty(method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute), inherit: false));
    }
}
