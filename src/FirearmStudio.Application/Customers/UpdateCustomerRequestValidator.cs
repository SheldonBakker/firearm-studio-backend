using FluentValidation;

namespace FirearmStudio.Application.Customers;

public sealed class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.FullName.Value)
            .NotEmpty()
            .OverridePropertyName(nameof(UpdateCustomerRequest.FullName))
            .When(x => x.FullName.IsSet);

        RuleFor(x => x.Email.Value)
            .NotEmpty().EmailAddress()
            .OverridePropertyName(nameof(UpdateCustomerRequest.Email))
            .When(x => x.Email.IsSet);

        RuleFor(x => x.Phone.Value)
            .NotEmpty()
            .OverridePropertyName(nameof(UpdateCustomerRequest.Phone))
            .When(x => x.Phone.IsSet);
    }
}
