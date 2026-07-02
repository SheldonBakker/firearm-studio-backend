using FluentValidation;

namespace FirearmStudio.Application.Contact.SubmitContactForm;

public sealed class ContactFormRequestValidator : AbstractValidator<ContactFormRequest>
{
    public ContactFormRequestValidator()
    {
        RuleFor(request => request.FullName)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
        RuleFor(request => request.Company)
            .MaximumLength(200);
        RuleFor(request => request.Message)
            .NotEmpty()
            .MaximumLength(4000);
    }
}
