using FirearmStudio.Domain.Authentication;
using FirearmStudio.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FirearmStudio.WebApi.Tests;

public sealed class CompanyControllerStorefrontAttributesTests
{
    private static readonly Type ControllerType = typeof(CompanyController);

    [Fact]
    public void GetStorefront_carries_Authorize_AdminOnly()
    {
        var method = ControllerType.GetMethod(nameof(CompanyController.GetStorefront));
        Assert.NotNull(method);

        var attr = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
        Assert.Equal(AppRoles.Policy.AdminOnly, attr.Roles);
    }

    [Fact]
    public void GetStorefront_carries_HttpGet_with_storefront_template()
    {
        var method = ControllerType.GetMethod(nameof(CompanyController.GetStorefront));
        Assert.NotNull(method);

        var attr = method.GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
            .Cast<HttpGetAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
        Assert.Equal("storefront", attr.Template);
    }

    [Fact]
    public void RotateStorefrontKey_carries_Authorize_AdminOnly()
    {
        var method = ControllerType.GetMethod(nameof(CompanyController.RotateStorefrontKey));
        Assert.NotNull(method);

        var attr = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
        Assert.Equal(AppRoles.Policy.AdminOnly, attr.Roles);
    }

    [Fact]
    public void RotateStorefrontKey_carries_HttpPost_with_storefront_key_template()
    {
        var method = ControllerType.GetMethod(nameof(CompanyController.RotateStorefrontKey));
        Assert.NotNull(method);

        var attr = method.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false)
            .Cast<HttpPostAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
        Assert.Equal("storefront/key", attr.Template);
    }

    [Fact]
    public void RevokeStorefrontKey_carries_Authorize_AdminOnly()
    {
        var method = ControllerType.GetMethod(nameof(CompanyController.RevokeStorefrontKey));
        Assert.NotNull(method);

        var attr = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
        Assert.Equal(AppRoles.Policy.AdminOnly, attr.Roles);
    }

    [Fact]
    public void RevokeStorefrontKey_carries_HttpDelete_with_storefront_key_template()
    {
        var method = ControllerType.GetMethod(nameof(CompanyController.RevokeStorefrontKey));
        Assert.NotNull(method);

        var attr = method.GetCustomAttributes(typeof(HttpDeleteAttribute), inherit: false)
            .Cast<HttpDeleteAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
        Assert.Equal("storefront/key", attr.Template);
    }
}
