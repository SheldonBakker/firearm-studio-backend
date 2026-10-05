namespace FirearmStudio.Application.Abstractions.Email;

public sealed record ContactFormAcknowledgementEmail(
    string RecipientEmail, string? RecipientName) : EmailMessage;
