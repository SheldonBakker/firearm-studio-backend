using FirearmStudio.Domain.Services;
using FluentValidation;

namespace FirearmStudio.Application.Bookings.UpdateAttendee;

public sealed class UpdateAttendeeRequestValidator : AbstractValidator<UpdateAttendeeRequest>
{
    public UpdateAttendeeRequestValidator()
    {
        RuleFor(request => request)
            .Must(request => request.FullName.IsSet
                             || request.IdNumber.IsSet
                             || request.LicenceNumber.IsSet
                             || request.FirearmMakeModel.IsSet
                             || request.FirearmSerialNumber.IsSet
                             || request.Calibre.IsSet
                             || request.FirearmOrigin.IsSet
                             || request.SignedIndemnity.IsSet
                             || request.Notes.IsSet)
            .WithMessage("At least one field must be supplied.");

        RuleFor(request => request.FullName.Value)
            .NotEmpty()
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdateAttendeeRequest.FullName))
            .When(request => request.FullName.IsSet);

        RuleFor(request => request.IdNumber.Value)
            .NotEmpty()
            .MaximumLength(20)
            .Must(SouthAfricanIdValidator.IsValid)
            .WithMessage("IdNumber must be a valid South African ID number or passport number.")
            .OverridePropertyName(nameof(UpdateAttendeeRequest.IdNumber))
            .When(request => request.IdNumber.IsSet);

        RuleFor(request => request.LicenceNumber.Value)
            .MaximumLength(50)
            .OverridePropertyName(nameof(UpdateAttendeeRequest.LicenceNumber))
            .When(request => request.LicenceNumber.IsSet);

        RuleFor(request => request.FirearmMakeModel.Value)
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdateAttendeeRequest.FirearmMakeModel))
            .When(request => request.FirearmMakeModel.IsSet);

        RuleFor(request => request.FirearmSerialNumber.Value)
            .MaximumLength(100)
            .OverridePropertyName(nameof(UpdateAttendeeRequest.FirearmSerialNumber))
            .When(request => request.FirearmSerialNumber.IsSet);

        RuleFor(request => request.Calibre.Value)
            .MaximumLength(50)
            .OverridePropertyName(nameof(UpdateAttendeeRequest.Calibre))
            .When(request => request.Calibre.IsSet);

        RuleFor(request => request.FirearmOrigin.Value)
            .IsInEnum()
            .OverridePropertyName(nameof(UpdateAttendeeRequest.FirearmOrigin))
            .When(request => request.FirearmOrigin.IsSet);

        RuleFor(request => request.Notes.Value)
            .MaximumLength(500)
            .OverridePropertyName(nameof(UpdateAttendeeRequest.Notes))
            .When(request => request.Notes.IsSet);
    }
}
