using FirearmStudio.Application.Bookings;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Abstractions;

public interface IBookingRequestedOutbox
{
    void Add(Company company, string email, string? fullName, PublicBookingResponse response);
}
