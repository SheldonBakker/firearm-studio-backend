using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Abstractions;

public interface IBookingLifecycleOutbox
{
    void Add(
        string messageType,
        Company company,
        Booking booking,
        string? rangeName,
        string email,
        string? fullName,
        string? icsUrl,
        string? googleCalendarUrl,
        decimal? depositAmount,
        DateTime? depositDueAt,
        string? invoiceNumber);
}
