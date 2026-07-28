using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.CheckInBooking;

public sealed record CheckInBookingCommand(Guid Id, CheckInBookingRequest Request) : ICommand<ErrorOr<Updated>>;
