using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.CreatePublicBooking;

public sealed record CreatePublicBookingCommand(Guid CompanyId, CreatePublicBookingRequest Request)
    : ICommand<ErrorOr<PublicBookingConfirmationResponse>>;
