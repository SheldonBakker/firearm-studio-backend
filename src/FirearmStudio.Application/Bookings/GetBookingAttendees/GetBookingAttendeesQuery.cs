using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.GetBookingAttendees;

public sealed record GetBookingAttendeesQuery(Guid BookingId) : IQuery<ErrorOr<IReadOnlyList<AttendeeResponse>>>;
