using FluentValidation;

namespace FirearmStudio.Application.Customers.UpdateCustomer;

public sealed class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(request => request)
            .Must(request => request.FullName.IsSet
                             || request.CompanyName.IsSet
                             || request.Email.IsSet
                             || request.Phone.IsSet
                             || request.Notes.IsSet
                             || request.IsActive.IsSet)
            .WithMessage("At least one field must be supplied.");

        RuleFor(request => request.FullName.Value)
            .NotEmpty()
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdateCustomerRequest.FullName))
            .When(request => request.FullName.IsSet);
        RuleFor(request => request.CompanyName.Value)
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdateCustomerRequest.CompanyName))
            .When(request => request.CompanyName.IsSet);
        RuleFor(request => request.Email.Value)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320)
            .OverridePropertyName(nameof(UpdateCustomerRequest.Email))
            .When(request => request.Email.IsSet);
        RuleFor(request => request.Phone.Value)
            .NotEmpty()
            .MaximumLength(50)
            .OverridePropertyName(nameof(UpdateCustomerRequest.Phone))
            .When(request => request.Phone.IsSet);
        RuleFor(request => request.Notes.Value)
            .MaximumLength(4000)
            .OverridePropertyName(nameof(UpdateCustomerRequest.Notes))
            .When(request => request.Notes.IsSet);
    }
}
