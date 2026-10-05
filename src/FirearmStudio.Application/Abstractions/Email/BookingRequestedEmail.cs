namespace FirearmStudio.Application.Abstractions.Email;

public sealed record BookingRequestedEmail(
    string RecipientEmail, string? CustomerName,
    string InvoiceNumber, decimal Subtotal, decimal VatAmount, decimal Total,
    IReadOnlyList<BookingEmailSession> Sessions, CompanyEmailDetails Company) : EmailMessage;
