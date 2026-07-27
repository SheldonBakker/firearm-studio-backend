using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.UpdateAttendee;

public sealed record UpdateAttendeeCommand(Guid Id, UpdateAttendeeRequest Request) : ICommand<ErrorOr<Updated>>;
