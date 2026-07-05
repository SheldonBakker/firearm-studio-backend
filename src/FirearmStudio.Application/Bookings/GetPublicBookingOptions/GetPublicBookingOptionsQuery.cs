using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Bookings.GetPublicBookingOptions;

public sealed record GetPublicBookingOptionsQuery(Guid CompanyId) : IQuery<ErrorOr<PublicBookingOptionsResponse>>;
