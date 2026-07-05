using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.GetBooking;

public sealed record GetBookingQuery(Guid Id) : IQuery<ErrorOr<BookingResponse>>;
