using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Bookings.GetBookings;

public sealed record GetBookingsQuery(
    int PageNumber,
    int PageSize,
    string SortOrder,
    Guid? ShootingRangeId,
    BookingStatus? Status,
    Guid? CustomerId,
    DateOnly? DateFrom,
    DateOnly? DateTo) : IQuery<ErrorOr<PaginatedResponse<BookingListItemDto>>>;
