using FluentValidation;

namespace FirearmStudio.Application.Bookings.CreatePublicBooking;

public sealed class CreatePublicBookingRequestValidator : AbstractValidator<CreatePublicBookingRequest>
{
    public const int MaxDaysInAdvance = 90;

    public CreatePublicBookingRequestValidator()
    {
        RuleFor(request => request.ShootingRangeId).NotEmpty();
        RuleFor(request => request.PackageId).NotEmpty();
        RuleFor(request => request.ShooterCount).InclusiveBetween(1, 20);
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Phone).MaximumLength(50);
        RuleFor(request => request.Notes).MaximumLength(2000);

        RuleFor(request => request.BookingDate)
            .Must(date =>
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
                return date >= today && date <= today.AddDays(MaxDaysInAdvance);
            })
            .WithMessage($"Booking date must be within the next {MaxDaysInAdvance} days.");
    }
}
