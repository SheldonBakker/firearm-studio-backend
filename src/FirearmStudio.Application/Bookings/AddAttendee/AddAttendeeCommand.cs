using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.AddAttendee;

public sealed record AddAttendeeCommand(Guid BookingId, AttendeeRequest Request) : ICommand<ErrorOr<Guid>>;
