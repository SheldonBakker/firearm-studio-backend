using FluentValidation;

namespace FirearmStudio.Application.Bookings.CreateBooking;

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(request => request.ShootingRangeId).NotEmpty();
        RuleFor(request => request.PackageId).NotEmpty();
        RuleFor(request => request.CustomerId).NotEmpty();
        RuleFor(request => request.ShooterCount).InclusiveBetween(1, 20);
        RuleFor(request => request.Notes).MaximumLength(2000);
    }
}
