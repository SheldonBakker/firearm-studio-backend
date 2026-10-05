namespace FirearmStudio.Application.Abstractions.Email;

public sealed record BookingEmailSession(
    string BookingNumber, DateOnly Date, TimeOnly Start, TimeOnly End,
    string? RangeName, string PackageName, decimal PackagePrice,
    string? IcsUrl, string? GoogleCalendarUrl, decimal? DepositAmount, DateTime? DepositDueAt);
