using FluentValidation;

namespace FirearmStudio.Application.Firearms.CreateFirearm;

public sealed class CreateFirearmRequestValidator : AbstractValidator<CreateFirearmRequest>
{
    public CreateFirearmRequestValidator()
    {
        RuleFor(request => request.CustomerId).NotEmpty();
        RuleFor(request => request.Make).NotEmpty().MaximumLength(120);
        RuleFor(request => request.Model).MaximumLength(120);
        RuleFor(request => request.Calibre).MaximumLength(80);
        RuleFor(request => request.FirearmType).MaximumLength(80);
        RuleFor(request => request.SerialNumber).NotEmpty().MaximumLength(120);
        RuleFor(request => request.InternalReference).MaximumLength(120);
        RuleFor(request => request.Notes).MaximumLength(4000);
    }
}
