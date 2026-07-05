using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.CompleteBooking;

public sealed record CompleteBookingCommand(Guid Id) : ICommand<ErrorOr<Updated>>;
