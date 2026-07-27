using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.RemoveAttendee;

public sealed record RemoveAttendeeCommand(Guid Id) : ICommand<ErrorOr<Deleted>>;
