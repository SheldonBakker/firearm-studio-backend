using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.ConfirmBooking;

public sealed record ConfirmBookingCommand(Guid Id) : ICommand<ErrorOr<Updated>>;
