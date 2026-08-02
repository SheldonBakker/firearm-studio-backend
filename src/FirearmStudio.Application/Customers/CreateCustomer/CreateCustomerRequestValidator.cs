using FirearmStudio.Domain.Enums;
using FirearmStudio.Domain.Services;
using FluentValidation;

namespace FirearmStudio.Application.Customers.CreateCustomer;

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(request => request.CustomerType).IsInEnum();
        RuleFor(request => request.FullName)
            .NotEmpty()
            .When(request => request.CustomerType == CustomerType.Individual);
        RuleFor(request => request.CompanyName)
            .NotEmpty()
            .When(request => request.CustomerType == CustomerType.Company);
        RuleFor(request => request.FullName).MaximumLength(200);
        RuleFor(request => request.CompanyName).MaximumLength(200);
        RuleFor(request => request.RegistrationNumber).MaximumLength(50);
        RuleFor(request => request.VatNumber).MaximumLength(50);
        RuleFor(request => request.Email)
            .EmailAddress()
            .MaximumLength(320)
            .When(request => !string.IsNullOrWhiteSpace(request.Email));
        RuleFor(request => request.Phone).MaximumLength(50);
        RuleFor(request => request.AddressLine1).MaximumLength(200);
        RuleFor(request => request.City).MaximumLength(120);
        RuleFor(request => request.Province).MaximumLength(120);
        RuleFor(request => request.PostalCode).MaximumLength(20);
        RuleFor(request => request.Notes).MaximumLength(4000);
        RuleFor(request => request.IdNumber)
            .MaximumLength(20)
            .Must(idNumber => SouthAfricanIdValidator.IsValid(idNumber!))
            .WithMessage("IdNumber must be a valid South African ID number or passport number.")
            .When(request => !string.IsNullOrWhiteSpace(request.IdNumber));
    }
}
