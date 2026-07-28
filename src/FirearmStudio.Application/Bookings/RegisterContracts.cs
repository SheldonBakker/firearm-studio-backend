using System.Linq.Expressions;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Bookings;

public sealed record RegisterRowDto(
    DateOnly BookingDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string RangeName,
    string BookingNumber,
    string? CustomerName,
    string AttendeeFullName,
    string AttendeeIdNumber,
    string? LicenceNumber,
    string? FirearmMakeModel,
    string? FirearmSerialNumber,
    string? Calibre,
    FirearmOrigin FirearmOrigin,
    bool SignedIndemnity,
    DateTime? CheckedInAt)
{
    public static Expression<Func<BookingAttendee, RegisterRowDto>> QueryProjection => a => new RegisterRowDto(
        a.Booking!.BookingDate,
        a.Booking.StartTime,
        a.Booking.EndTime,
        a.Booking.ShootingRange!.Name,
        a.Booking.BookingNumber,
        a.Booking.Customer!.CustomerType == CustomerType.Company ? a.Booking.Customer.CompanyName : a.Booking.Customer.FullName,
        a.FullName,
        a.IdNumber,
        a.LicenceNumber,
        a.FirearmMakeModel,
        a.FirearmSerialNumber,
        a.Calibre,
        a.FirearmOrigin,
        a.SignedIndemnity,
        a.Booking.CheckedInAt);
}
