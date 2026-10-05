namespace FirearmStudio.Application.Abstractions.Email;

public sealed record BookingLifecycleEmail(
    BookingLifecycleKind Kind, string RecipientEmail, string? CustomerName,
    BookingEmailSession Session, int ShooterCount, string? InvoiceNumber,
    CompanyEmailDetails Company) : EmailMessage;
