using FirearmStudio.Domain.Services;
using FluentValidation;

namespace FirearmStudio.Application.Bookings.AddAttendee;

public sealed class AttendeeRequestValidator : AbstractValidator<AttendeeRequest>
{
    public AttendeeRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.IdNumber)
            .NotEmpty()
            .MaximumLength(20)
            .Must(SouthAfricanIdValidator.IsValid)
            .WithMessage("IdNumber must be a valid South African ID number or passport number.");
        RuleFor(request => request.LicenceNumber).MaximumLength(50);
        RuleFor(request => request.FirearmMakeModel).MaximumLength(200);
        RuleFor(request => request.FirearmSerialNumber).MaximumLength(100);
        RuleFor(request => request.Calibre).MaximumLength(50);
        RuleFor(request => request.FirearmOrigin).IsInEnum();
        RuleFor(request => request.Notes).MaximumLength(500);
    }
}
