using FirearmStudio.Application.Bookings.AddAttendee;
using FluentValidation;

namespace FirearmStudio.Application.Bookings.CheckInBooking;

public sealed class CheckInBookingRequestValidator : AbstractValidator<CheckInBookingRequest>
{
    public CheckInBookingRequestValidator()
    {
        RuleFor(request => request.Attendees).NotEmpty();
        RuleForEach(request => request.Attendees).SetValidator(new AttendeeRequestValidator());
    }
}
