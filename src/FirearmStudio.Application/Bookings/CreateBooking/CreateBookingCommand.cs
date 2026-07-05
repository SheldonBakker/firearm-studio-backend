using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.CreateBooking;

public sealed record CreateBookingCommand(CreateBookingRequest Request) : ICommand<ErrorOr<BookingResponse>>;
