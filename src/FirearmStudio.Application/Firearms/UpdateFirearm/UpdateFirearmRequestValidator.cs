using FluentValidation;

namespace FirearmStudio.Application.Firearms.UpdateFirearm;

public sealed class UpdateFirearmRequestValidator : AbstractValidator<UpdateFirearmRequest>
{
    public UpdateFirearmRequestValidator()
    {
        RuleFor(request => request)
            .Must(request => request.Model.IsSet
                             || request.Calibre.IsSet
                             || request.FirearmType.IsSet
                             || request.Notes.IsSet
                             || request.Status.IsSet)
            .WithMessage("At least one field must be supplied.");
        RuleFor(request => request.Model.Value)
            .MaximumLength(120)
            .OverridePropertyName(nameof(UpdateFirearmRequest.Model))
            .When(request => request.Model.IsSet);
        RuleFor(request => request.Calibre.Value)
            .MaximumLength(80)
            .OverridePropertyName(nameof(UpdateFirearmRequest.Calibre))
            .When(request => request.Calibre.IsSet);
        RuleFor(request => request.FirearmType.Value)
            .MaximumLength(80)
            .OverridePropertyName(nameof(UpdateFirearmRequest.FirearmType))
            .When(request => request.FirearmType.IsSet);
        RuleFor(request => request.Notes.Value)
            .MaximumLength(4000)
            .OverridePropertyName(nameof(UpdateFirearmRequest.Notes))
            .When(request => request.Notes.IsSet);
        RuleFor(request => request.Status.Value)
            .IsInEnum()
            .OverridePropertyName(nameof(UpdateFirearmRequest.Status))
            .When(request => request.Status.IsSet);
    }
}
