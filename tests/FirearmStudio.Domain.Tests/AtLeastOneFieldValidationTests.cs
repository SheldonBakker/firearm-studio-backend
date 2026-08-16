using FirearmStudio.Application.Customers;
using FirearmStudio.Application.Customers.UpdateCustomer;
using FirearmStudio.Application.Firearms;
using FirearmStudio.Application.Firearms.UpdateFirearm;
using FirearmStudio.Application.Model;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class AtLeastOneFieldValidationTests
{
    [Fact]
    public void UpdateFirearm_empty_request_produces_at_least_one_field_error()
    {
        var validator = new UpdateFirearmRequestValidator();
        var request = new UpdateFirearmRequest(default, default, default, default, default);

        var result = validator.Validate(request);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "At least one field must be supplied.");
    }

    [Fact]
    public void UpdateCustomer_empty_request_produces_at_least_one_field_error()
    {
        var validator = new UpdateCustomerRequestValidator();
        var request = new UpdateCustomerRequest(default, default, default, default, default, default, default);

        var result = validator.Validate(request);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "At least one field must be supplied.");
    }

    [Fact]
    public void UpdateFirearm_empty_request_error_has_empty_property_name()
    {
        var validator = new UpdateFirearmRequestValidator();
        var request = new UpdateFirearmRequest(default, default, default, default, default);

        var result = validator.Validate(request);

        var error = Assert.Single(result.Errors, e => e.ErrorMessage == "At least one field must be supplied.");
        Assert.Equal(string.Empty, error.PropertyName);
    }

    [Fact]
    public void UpdateCustomer_empty_request_error_has_empty_property_name()
    {
        var validator = new UpdateCustomerRequestValidator();
        var request = new UpdateCustomerRequest(default, default, default, default, default, default, default);

        var result = validator.Validate(request);

        var error = Assert.Single(result.Errors, e => e.ErrorMessage == "At least one field must be supplied.");
        Assert.Equal(string.Empty, error.PropertyName);
    }

    [Fact]
    public void UpdateFirearm_request_with_one_set_field_does_not_produce_at_least_one_field_error()
    {
        var validator = new UpdateFirearmRequestValidator();
        var request = new UpdateFirearmRequest(
            Model: new Optional<string?>("Glock 17"),
            Calibre: default,
            FirearmType: default,
            Notes: default,
            Status: default);

        var result = validator.Validate(request);

        Assert.DoesNotContain(result.Errors, e => e.ErrorMessage == "At least one field must be supplied.");
    }

    [Fact]
    public void UpdateCustomer_request_with_one_set_field_does_not_produce_at_least_one_field_error()
    {
        var validator = new UpdateCustomerRequestValidator();
        var request = new UpdateCustomerRequest(
            FullName: new Optional<string>("John"),
            CompanyName: default,
            Email: default,
            Phone: default,
            Notes: default,
            IsActive: default,
            IdNumber: default);

        var result = validator.Validate(request);

        Assert.DoesNotContain(result.Errors, e => e.ErrorMessage == "At least one field must be supplied.");
    }
}
