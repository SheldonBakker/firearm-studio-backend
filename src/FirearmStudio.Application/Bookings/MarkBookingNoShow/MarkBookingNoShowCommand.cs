using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.MarkBookingNoShow;

public sealed record MarkBookingNoShowCommand(Guid Id) : ICommand<ErrorOr<Updated>>;
