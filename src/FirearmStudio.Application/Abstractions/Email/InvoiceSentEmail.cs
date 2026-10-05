namespace FirearmStudio.Application.Abstractions.Email;

public sealed record InvoiceSentEmail(
    string RecipientEmail, string? CustomerName,
    string InvoiceNumber, DateOnly InvoiceMonth, DateOnly? DueOn,
    decimal Subtotal, decimal VatAmount, decimal Total,
    IReadOnlyList<InvoiceEmailLine> Lines, CompanyEmailDetails Company) : EmailMessage;
