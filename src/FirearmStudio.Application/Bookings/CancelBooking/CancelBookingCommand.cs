using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.CancelBooking;

public sealed record CancelBookingCommand(Guid Id) : ICommand<ErrorOr<Updated>>;
