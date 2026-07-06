using FluentValidation;

namespace FirearmStudio.Application.Bookings.CreatePublicBooking;

public sealed class CreatePublicBookingRequestValidator : AbstractValidator<CreatePublicBookingRequest>
{
    public const int MaxDaysInAdvance = 90;
    public const int MaxSessionsPerBooking = 20;

    public CreatePublicBookingRequestValidator()
    {
        RuleFor(request => request.Sessions)
            .NotEmpty()
            .Must(sessions => sessions.Count <= MaxSessionsPerBooking)
            .WithMessage($"A booking may contain at most {MaxSessionsPerBooking} session(s).");

        RuleForEach(request => request.Sessions).SetValidator(new PublicBookingSessionRequestValidator());

        RuleFor(request => request.FullName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Phone).MaximumLength(50);
        RuleFor(request => request.Notes).MaximumLength(2000);
    }
}

public sealed class PublicBookingSessionRequestValidator : AbstractValidator<PublicBookingSessionRequest>
{
    public PublicBookingSessionRequestValidator()
    {
        RuleFor(session => session.ShootingRangeId).NotEmpty();
        RuleFor(session => session.PackageId).NotEmpty();
        RuleFor(session => session.ShooterCount).InclusiveBetween(1, 20);
        RuleFor(session => session.Notes).MaximumLength(2000);

        RuleFor(session => session.BookingDate)
            .Must(date =>
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
                return date >= today && date <= today.AddDays(CreatePublicBookingRequestValidator.MaxDaysInAdvance);
            })
            .WithMessage($"Booking date must be within the next {CreatePublicBookingRequestValidator.MaxDaysInAdvance} days.");
    }
}
