using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.GetBookingIcs;

public sealed record GetBookingIcsQuery(string Token) : IQuery<ErrorOr<byte[]>>;
